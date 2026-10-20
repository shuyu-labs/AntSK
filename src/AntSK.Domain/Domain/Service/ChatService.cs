using AntSK.Domain.Domain.Other.Search;
using System.Runtime.CompilerServices;
using AntSK.Domain.Common.DependencyInjection;
using AntSK.Domain.Domain.Interface;
using AntSK.Domain.Domain.Model;
using AntSK.Domain.Domain.Model.Constant;
using AntSK.Domain.Domain.Model.Dto;
using AntSK.Domain.Domain.Other.Bge;
using AntSK.Domain.Repositories;
using AntSK.Domain.Utils;
using AntSK.LLM.StableDiffusion;
using Markdig;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using ChatHistory = Microsoft.SemanticKernel.ChatCompletion.ChatHistory;

namespace AntSK.Domain.Domain.Service
{
    [ServiceDescription(typeof(IChatService), ServiceLifetime.Scoped)]
    public class ChatService(
        IKernelService _kernelService,
        IKMService _kMService,
        IKmsDetails_Repositories _kmsDetails_Repositories,
        IAIModels_Repositories _aIModels_Repositories,
        IHttpClientFactory _httpClientFactory
        ) : IChatService
    {
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="app"></param>
        /// <param name="history"></param>
        /// <param name="searchConversation">会话拥有的搜索标识；为空时随 history 生命周期创建。</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<string> SendChatByAppAsync(
            Apps app, ChatHistory history, WebSearchConversation? searchConversation = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var kernel = _kernelService.GetKernelByApp(app);
            var settings = new OpenAIPromptExecutionSettings { Temperature = app.Temperature / 100 };
            bool hasCustomTools = !string.IsNullOrEmpty(app.ApiFunctionList) || !string.IsNullOrEmpty(app.NativeFunctionList);
            if (hasCustomTools) _kernelService.ImportFunctionsByApp(app, kernel);
            using var httpClient = _httpClientFactory.CreateClient("ParallelSearch");
            var conversation = searchConversation ?? WebSearchConversation.ForHistory(history);
            await foreach (var content in ChatToolRunner.RunAsync(kernel, history, settings,
                app.EnableWebSearch == true, hasCustomTools, httpClient, conversation.Id, cancellationToken))
                yield return content;
        }

        public async IAsyncEnumerable<StreamingKernelContent> SendKmsByAppAsync(Apps app, string questions, ChatHistory history, string filePath, List<RelevantSource> relevantSources = null)
        {
            relevantSources?.Clear();
            List<RelevantSource> relevantSourceList = new List<RelevantSource>();
            
            var _kernel = _kernelService.GetKernelByApp(app);
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                //上传文件问答
                var memory = _kMService.GetMemoryByApp(app);

                // 匹配GUID的正则表达式
                string pattern = @"\b[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}\b";

                // 使用正则表达式找到匹配
                Match match = Regex.Match(filePath, pattern);
                if (match.Success)
                {
                    var fileId = match.Value;

                    var status = await memory.IsDocumentReadyAsync(fileId, index: KmsConstantcs.KmsIndex);
                    if (!status)
                    {
                        var result = await memory.ImportDocumentAsync(new Document(fileId).AddFile(filePath)
                                  .AddTag(KmsConstantcs.AppIdTag, app.Id)
                                  .AddTag(KmsConstantcs.FileIdTag, fileId)
                                  , index: KmsConstantcs.FileIndex);
                    }

                    var filters = new List<MemoryFilter>() {
                        new MemoryFilter().ByTag(KmsConstantcs.AppIdTag, app.Id),
                        new MemoryFilter().ByTag(KmsConstantcs.FileIdTag, fileId)
                    };

                    var searchResult = await memory.SearchAsync(questions, index: KmsConstantcs.FileIndex, filters: filters);
                    relevantSourceList.AddRange(searchResult.Results.SelectMany(item => item.Partitions.Select(part => new RelevantSource()
                    {
                        SourceName = item.SourceName,
                        Text = Markdown.ToHtml(part.Text),
                        Relevance = part.Relevance
                    })));
                    app.Prompt = KmsConstantcs.KmsPrompt;
                }
            }
            else 
            {
                //从知识库问答
                relevantSourceList = await _kMService.GetRelevantSourceList(app, questions);
            }


            var dataMsg = new StringBuilder();
            if (relevantSourceList.Any())
            {
                if (!string.IsNullOrEmpty(app.RerankModelID))
                {
                    var rerankModel=_aIModels_Repositories.GetById(app.RerankModelID);
                    BegRerankConfig.LoadModel(rerankModel.EndPoint, rerankModel.ModelName);
                    //进行rerank
                    foreach (var item in relevantSourceList)
                    {
                        List<string> rerank = new List<string>();
                        rerank.Add(questions);
                        rerank.Add(item.Text);
                        item.RerankScore = BegRerankConfig.Rerank(rerank);
                      
                    }
                    relevantSourceList = relevantSourceList.OrderByDescending(p => p.RerankScore).Take(app.MaxMatchesCount).ToList();
                }
                    
                bool isSearch = false;
                foreach (var item in relevantSourceList)
                {
                    if (!string.IsNullOrEmpty(app.RerankModelID))
                    {
                        //匹配重排后相似度
                        if (item.RerankScore >= app.Relevance / 100)
                        {
                            dataMsg.AppendLine(item.ToString());
                            isSearch = true;
                        }
                    }
                    else 
                    {
                        //匹配相似度
                        if (item.Relevance >= app.Relevance / 100)
                        {
                            dataMsg.AppendLine(item.ToString());
                            isSearch = true;
                        }
                    }
                }

                //处理markdown显示
                relevantSources?.AddRange(relevantSourceList);
                Dictionary<string, string> fileDic = new Dictionary<string, string>();
                foreach (var item in relevantSourceList)
                {
                    if (fileDic.ContainsKey(item.SourceName))
                    {
                        item.SourceName = fileDic[item.SourceName];
                    }
                    else
                    {
                        var fileDetail = _kmsDetails_Repositories.GetFirst(p => p.FileGuidName == item.SourceName);
                        if (fileDetail.IsNotNull())
                        {
                            string fileName = fileDetail.FileName;
                            fileDic.Add(item.SourceName, fileName);
                            item.SourceName = fileName;
                        }       
                    }
                    item.Text = Markdown.ToHtml(item.Text);
                }

                if (isSearch)
                {
                    //KernelFunction jsonFun = _kernel.Plugins.GetFunction("KMSPlugin", "Ask1");
                    var temperature = app.Temperature / 100;//存的是0~100需要缩小
                    OpenAIPromptExecutionSettings settings = new() { Temperature = temperature };
                    var func = _kernel.CreateFunctionFromPrompt(app.Prompt , settings);

                    var chatResult = _kernel.InvokeStreamingAsync(function: func,
                        arguments: new KernelArguments() { ["doc"] = dataMsg.ToString(), ["history"] = string.Join("\n", history.Select(x => x.Role + ": " + x.Content)), ["input"] = questions });

                    await foreach (var content in chatResult)
                    {
                        yield return content;
                    }
                }
                else
                {
                    yield return new StreamingTextContent(KmsConstantcs.KmsSearchNull);
                }
            }
            else
            {
                yield return new StreamingTextContent(KmsConstantcs.KmsSearchNull);
            }
        }


        public async Task<ChatHistory> GetChatHistory(List<Chats> MessageList, ChatHistory history)
        {
            foreach (var item in MessageList)
            {
                if (item.IsSend)
                {
                    history.AddUserMessage(item.Context);
                }
                else
                {
                    history.AddAssistantMessage(item.Context);
                }
            }
            return history;
        }
    }
}
