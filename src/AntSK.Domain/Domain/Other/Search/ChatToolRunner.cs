using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AntSK.Domain.Domain.Other.Search;

/// <summary>Runs the chat application's selected tools and consumes their results.</summary>
public static class ChatToolRunner
{
    public static async IAsyncEnumerable<string> RunAsync(
        Kernel kernel, ChatHistory history, OpenAIPromptExecutionSettings settings,
        bool enableWebSearch, bool hasCustomTools, HttpClient httpClient, Guid conversationId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chat = kernel.GetRequiredService<IChatCompletionService>();
        // No connection is made unless the model actually calls a web tool.
        await using var search = new ParallelSearchPlugin(httpClient, conversationId);
        if (enableWebSearch) kernel.ImportPluginFromObject(search, "ParallelSearch");

        if (!enableWebSearch && !hasCustomTools)
        {
            await foreach (var content in chat.GetStreamingChatMessageContentsAsync(history, settings, kernel, cancellationToken))
                yield return content.Content ?? "";
            yield break;
        }

        var allowedFunctions = kernel.Plugins.GetFunctionsMetadata().Where(function =>
            (enableWebSearch && function.PluginName == "ParallelSearch") ||
            (hasCustomTools && function.PluginName == "AntSKFunctions")).ToArray();
        settings.ToolCallBehavior = ToolCallBehavior.EnableFunctions(allowedFunctions.Select(function => function.ToOpenAIFunction()));
        // Bound tool use so a model that repeatedly requests tools cannot loop forever.
        for (var turn = 0; turn < 10; turn++)
        {
            var result = await chat.GetChatMessageContentAsync(history, settings, kernel, cancellationToken);
            var calls = FunctionCallContent.GetFunctionCalls(result).ToArray();
            if (calls.Length == 0)
            {
                if (result.Content is not null) yield return result.Content;
                yield break;
            }

            history.Add(result);
            foreach (var call in calls)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!allowedFunctions.Any(function => function.PluginName == call.PluginName && function.Name == call.FunctionName))
                {
                    history.Add(new FunctionResultContent(call, "This tool is not enabled for this application.").ToChatMessage());
                    continue;
                }
                var output = await call.InvokeAsync(kernel, cancellationToken);
                history.Add(output.ToChatMessage());
            }
        }
        yield return "已达到本轮工具调用上限，请缩小问题范围后重试。";
    }
}
