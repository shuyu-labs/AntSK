using AntSK.Domain.Common.DependencyInjection;
using AntSK.Domain.Domain.Interface;
using AntSK.Domain.Domain.Model.Dto.OpenAPI;
using AntSK.Domain.Domain.Other.Bge;
using AntSK.Domain.Repositories;
using AntSK.Domain.Utils;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;
using ServiceLifetime = AntSK.Domain.Common.DependencyInjection.ServiceLifetime;

namespace AntSK.Services.OpenApi
{
    public interface IOpenApiService
    {
        Task Chat(OpenAIModel model, string sk, HttpContext HttpContext);

        Task<double> Rerank(RerankModel model, string sk, HttpContext HttpContext);
    }

    [ServiceDescription(typeof(IOpenApiService), ServiceLifetime.Scoped)]
    public class OpenApiService(
        IApps_Repositories _apps_Repositories,
        IKernelService _kernelService,
        IKMService _kMService,
        IChatService _chatService,
        IAIModels_Repositories _aIModels_Repositories
    ) : IOpenApiService
    {
        public async Task Chat(OpenAIModel model, string sk, HttpContext HttpContext)
        {
            string headerValue = sk;
            Regex regex = new Regex(@"Bearer (.*)");
            Match match = regex.Match(headerValue);
            string token = match.Groups[1].Value;
            string questions;
            ChatHistory history;
            Apps app = _apps_Repositories.GetFirst(p => p.SecretKey == token);
            if (app.IsNotNull())
            {

                switch (app.Type)
                {
                    case "chat":
                        (questions, history) = await GetHistory(model,app.Prompt);
                        //普通会话
                        history.AddUserMessage(questions);
                        if (model.stream)
                        {
                            OpenAIStreamResult result1 = new OpenAIStreamResult();
                            result1.created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            result1.choices = new List<StreamChoicesModel>()
                                { new StreamChoicesModel() { delta = new OpenAIMessage() { role = "assistant" } } };
                            await SendChatStream(HttpContext, result1, app,history);
                            return;
                        }
                        else
                        {
                            OpenAIResult result2 = new OpenAIResult();
                            result2.created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            result2.choices = new List<ChoicesModel>()
                                { new ChoicesModel() { message = new OpenAIMessage() { role = "assistant" } } };
                            result2.choices[0].message.content = await SendChat(history, app, HttpContext.RequestAborted);
                            HttpContext.Response.ContentType = "application/json";
                            await HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(result2));
                            await HttpContext.Response.CompleteAsync();
                        }
                        break;
                    case "kms":
                        (questions, history) = await GetHistory(model,"");
                        //知识库问答
                        if (model.stream)
                        {
                            OpenAIStreamResult result3 = new OpenAIStreamResult();
                            result3.created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            result3.choices = new List<StreamChoicesModel>()
                                { new StreamChoicesModel() { delta = new OpenAIMessage() { role = "assistant" } } };
                            await SendKmsStream(HttpContext, result3, app, questions,history);
                        }
                        else
                        {
                            OpenAIResult result4 = new OpenAIResult();
                            result4.created = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                            result4.choices = new List<ChoicesModel>()
                                { new ChoicesModel() { message = new OpenAIMessage() { role = "assistant" } } };
                            result4.choices[0].message.content = await SendKms(questions,history, app);
                            HttpContext.Response.ContentType = "application/json";
                            await HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(result4));
                            await HttpContext.Response.CompleteAsync();
                        }
                        break;
                }
            }
        }

        private async Task SendChatStream(HttpContext HttpContext, OpenAIStreamResult result, Apps app, ChatHistory history)
        {
            HttpContext.Response.Headers.Add("Content-Type", "text/event-stream;charset=utf-8");
            var chatResult = _chatService.SendChatByAppAsync(app, history, cancellationToken: HttpContext.RequestAborted);
            await foreach (var content in chatResult)
            {
                result.choices[0].delta.content = content.ConvertToString();
                string message = $"data: {JsonConvert.SerializeObject(result)}\n\n";
                await HttpContext.Response.WriteAsync(message, Encoding.UTF8);
                await HttpContext.Response.Body.FlushAsync();
                //模拟延迟。
                await Task.Delay(TimeSpan.FromMilliseconds(50));
            }

            await HttpContext.Response.WriteAsync("data: [DONE]");
            await HttpContext.Response.Body.FlushAsync();
            await HttpContext.Response.CompleteAsync();
        }

        /// <summary>
        /// 发送普通对话
        /// </summary>
        /// <param name="questions"></param>
        /// <param name="history"></param>
        /// <param name="app"></param>
        /// <returns></returns>
        private async Task<string> SendChat(ChatHistory history, Apps app, CancellationToken cancellationToken)
        {
            var result = new StringBuilder();
            await foreach (var content in _chatService.SendChatByAppAsync(app, history, cancellationToken: cancellationToken))
                result.Append(content);
            return result.ToString();
        }

        private async Task SendKmsStream(HttpContext HttpContext, OpenAIStreamResult result, Apps app, string questions,ChatHistory history)
        {
            HttpContext.Response.Headers.Add("Content-Type", "text/event-stream;charset=utf-8");
            var chatResult = _chatService.SendKmsByAppAsync(app, questions, history, "");
            int i = 0;
            await foreach (var content in chatResult)
            {
                result.choices[0].delta.content = content.ConvertToString();
                string message = $"data: {JsonConvert.SerializeObject(result)}\n\n";
                await HttpContext.Response.WriteAsync(message, Encoding.UTF8);
                await HttpContext.Response.Body.FlushAsync();
                //模拟延迟。
                await Task.Delay(TimeSpan.FromMilliseconds(50));
            }

            await HttpContext.Response.WriteAsync("data: [DONE]");
            await HttpContext.Response.Body.FlushAsync();

            await HttpContext.Response.CompleteAsync();
        }

        /// <summary>
        /// 发送知识库问答
        /// </summary>
        /// <param name="questions"></param>
        /// <param name="app"></param>
        /// <returns></returns>
        private async Task<string> SendKms(string questions, ChatHistory history, Apps app)
        {
            string result = "";
            var _kernel = _kernelService.GetKernelByApp(app);

            var relevantSource = await _kMService.GetRelevantSourceList(app, questions);
            var dataMsg = new StringBuilder();
            if (relevantSource.Any())
            {
                foreach (var item in relevantSource)
                {
                    dataMsg.AppendLine(item.ToString());
                }

                //KernelFunction jsonFun = _kernel.Plugins.GetFunction("KMSPlugin", "Ask1");
                var temperature = app.Temperature / 100;//存的是0~100需要缩小
                OpenAIPromptExecutionSettings settings = new() { Temperature = temperature };
                var func = _kernel.CreateFunctionFromPrompt(app.Prompt, settings);
                var chatResult = await _kernel.InvokeAsync(function: func,
                    arguments: new KernelArguments() { ["doc"] = dataMsg, ["history"] = string.Join("\n", history.Select(x => x.Role + ": " + x.Content)), ["input"] = questions });
                if (chatResult.IsNotNull())
                {
                    string answers = chatResult.GetValue<string>();
                    result = answers;
                }
            }

            return result;
        }

        /// <summary>
        /// 历史会话的会话总结
        /// </summary>
        /// <param name="app"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        private async Task<(string,ChatHistory)> GetHistory(OpenAIModel model,string systemPrompt)
        {
            ChatHistory history = new ChatHistory();
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                history = new ChatHistory(systemPrompt);
            }
            string questions = model.messages[model.messages.Count - 1].content;
            for (int i = 0; i < model.messages.Count()-1 ; i++)
            {
                var item = model.messages[i];
                if (item.role.ComparisonIgnoreCase("user"))
                {
                    history.AddUserMessage(item.content);
                }
                else if (item.role.ComparisonIgnoreCase("assistant"))
                {
                    history.AddAssistantMessage(item.content);
                }
                else if (item.role.ComparisonIgnoreCase("system"))
                {
                    history.AddSystemMessage(item.content);
                }
            }
            return (questions,history);
        }

        public async Task<double> Rerank(RerankModel model, string sk, HttpContext HttpContext)
        {
            var rerankModel = _aIModels_Repositories.GetById(model.modelId);
            BegRerankConfig.LoadModel(rerankModel.EndPoint, rerankModel.ModelName);
            List<string> rerank = new List<string>();
            rerank.Add(model.query);
            rerank.Add(model.document);
            var result= BegRerankConfig.Rerank(rerank);
            return result;
        }
    }
}