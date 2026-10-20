using System.Collections.Concurrent;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using AntSK.Domain.Domain.Interface;
using AntSK.Domain.Domain.Other.Search;
using AntSK.Domain.Domain.Service;
using AntSK.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Xunit;

namespace AntSK.Search.Tests;

public class ChatSearchTests
{
    [Fact]
    public async Task SearchFetchAndFollowupReachModelWithOneConversationId()
    {
        using var server = new McpHandler();
        var history = new ChatHistory();
        var model = new ScriptedModel("web_search", "web_fetch");
        var service = CreateService(server, model);
        var app = new Apps { EnableWebSearch = true };
        var output = await Run(service, app, history);
        Assert.Contains("https://example.org/source", output);
        Assert.Equal(2, model.ConsumedResults.Count);
        Assert.Contains("source excerpt", model.ConsumedResults[1]);

        // A later turn creates a new MCP client while preserving conversation identity.
        model.Enqueue("web_search");
        await Run(service, app, history);
        var calls = server.Calls.ToArray();
        Assert.Equal(new[] { "web_search", "web_fetch", "web_search" }, calls.Select(c => c.Name));
        Assert.Single(calls.Select(c => c.SessionId).Distinct());
        Assert.True(Guid.TryParse(calls[0].SessionId, out _));
        Assert.Equal("https://example.org/source", calls[1].Arguments.GetProperty("urls")[0].GetString());
        Assert.Contains(server.Requests, r => r.Method == "tools/list");
        Assert.All(server.Requests, r =>
        {
            Assert.Equal(ParallelSearchPlugin.Endpoint, r.Url);
            Assert.Equal(ParallelSearchPlugin.UserAgent, r.UserAgent);
            Assert.Null(r.Authorization);
        });
    }

    [Fact]
    public async Task IndependentAndResetConversationsStayIsolatedWithSharedHttpHandler()
    {
        using var server = new McpHandler();
        var serviceA = CreateService(server, new ScriptedModel("web_search", "web_fetch"));
        var serviceB = CreateService(server, new ScriptedModel("web_search", "web_fetch"));
        var historyA = new ChatHistory();
        var historyB = new ChatHistory();
        var app = new Apps { EnableWebSearch = true };
        await Task.WhenAll(Run(serviceA, app, historyA), Run(serviceB, app, historyB));
        var groups = server.Calls.GroupBy(c => c.SessionId).ToArray();
        Assert.Equal(2, groups.Length);
        Assert.All(groups, group => Assert.Equal(new[] { "web_search", "web_fetch" }, group.Select(c => c.Name)));
        var oldIds = groups.Select(g => g.Key).ToArray();

        // Reset/new-chat owns a fresh history; the default production resolver creates its ID.
        await Run(CreateService(server, new ScriptedModel("web_search")), app, new ChatHistory());
        Assert.DoesNotContain(server.Calls.Last().SessionId, oldIds);
    }

    [Fact]
    public async Task UiConversationSurvivesHistoryRebuildAndResetCreatesNewId()
    {
        using var server = new McpHandler();
        var model = new ScriptedModel("web_search");
        var service = CreateService(server, model);
        var app = new Apps { EnableWebSearch = true };
        var conversation = new WebSearchConversation();
        await Run(service, app, new ChatHistory(), conversation);
        // Blazor rebuilds history from saved messages and reloads its stored opaque context.
        conversation = JsonSerializer.Deserialize<WebSearchConversation>(JsonSerializer.Serialize(conversation))!;
        model.Enqueue("web_fetch");
        await Run(service, app, new ChatHistory(), conversation);
        Assert.Single(server.Calls.Select(c => c.SessionId).Distinct());
        model.Enqueue("web_search");
        await Run(service, app, new ChatHistory(), new WebSearchConversation());
        Assert.Equal(2, server.Calls.Select(c => c.SessionId).Distinct().Count());
    }

    [Fact]
    public async Task SavedOrDisabledAppsMakeNoRequestsAndKeepCustomTools()
    {
        using var server = new McpHandler();
        var model = new ScriptedModel();
        var service = CreateService(server, model);
        // Database default and model default preserve existing installations.
        Assert.NotEqual(true, new Apps().EnableWebSearch);
        Assert.Equal("streamed answer", await Run(service, new Apps(), new ChatHistory()));
        Assert.Empty(server.Requests);

        var customModel = new ScriptedModel("custom");
        await Run(CreateService(server, customModel), new Apps { ApiFunctionList = "saved-provider", EnableWebSearch = false }, new ChatHistory());
        Assert.Contains("custom result", Assert.Single(customModel.ConsumedResults));
        Assert.Empty(server.Requests);
        Assert.DoesNotContain("ParallelSearch", customModel.PluginNames);
    }

    [Fact]
    public async Task EnabledSearchDoesNotConnectIfModelDoesNotUseIt()
    {
        using var server = new McpHandler();
        var model = new ScriptedModel();
        await Run(CreateService(server, model), new Apps { EnableWebSearch = true }, new ChatHistory());
        Assert.Contains("ParallelSearch", model.PluginNames);
        Assert.Empty(server.Requests);
    }

    [Fact]
    public void SchemaUpgradePreservesSavedToolsAndLeavesWebSearchDisabled()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".db");
        try
        {
            using var db = new SqlSugar.SqlSugarClient(new SqlSugar.ConnectionConfig
            {
                ConnectionString = "Data Source=" + path,
                DbType = SqlSugar.DbType.Sqlite,
                IsAutoCloseConnection = true,
                InitKeyType = SqlSugar.InitKeyType.Attribute
            });
            db.CodeFirst.InitTables<Apps>();
            var legacyApp = new Apps
            {
                Id = "saved", Name = "saved", Describe = "saved", Type = "chat",
                ChatModelID = "saved-model", EmbeddingModelID = "saved-embedding",
                ApiFunctionList = "explicit-api", NativeFunctionList = "explicit-native"
            };
            // Existing SqlSugar metadata makes string columns NOT NULL even for nullable C# strings.
            foreach (var property in typeof(Apps).GetProperties().Where(p => p.PropertyType == typeof(string)))
                if (property.GetValue(legacyApp) is null) property.SetValue(legacyApp, "");
            db.Insertable(legacyApp).ExecuteCommand();
            // Recreate the pre-integration schema, then execute the same production CodeFirst upgrade.
            db.Ado.ExecuteCommand("ALTER TABLE Apps DROP COLUMN EnableWebSearch");
            db.CodeFirst.InitTables<Apps>();
            var saved = db.Queryable<Apps>().Single(app => app.Id == "saved");
            Assert.Null(saved.EnableWebSearch);
            Assert.Equal("explicit-api", saved.ApiFunctionList);
            Assert.Equal("explicit-native", saved.NativeFunctionList);
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData("http", "unavailable")]
    [InlineData("quota", "failed")]
    [InlineData("rpc", "protocol error")]
    [InlineData("timeout", "timed out")]
    public async Task ErrorsReachResultConsumerWithoutFallbackOrIdentityRotation(string mode, string expected)
    {
        using var server = new McpHandler { Mode = mode };
        var model = new ScriptedModel("web_search");
        var service = CreateService(server, model, mode == "timeout" ? TimeSpan.FromMilliseconds(100) : null);
        var history = new ChatHistory();
        var app = new Apps { EnableWebSearch = true };
        await Run(service, app, history);
        Assert.Contains(expected, Assert.Single(model.ConsumedResults));
        var firstId = Assert.Single(server.Calls).SessionId;
        server.Mode = "success";
        model.Enqueue("web_search");
        await Run(service, app, history);
        Assert.Equal(firstId, server.Calls.Last().SessionId);
    }

    [Fact]
    public async Task CancellationReachesTheTransportAndStopsTheConversation()
    {
        using var server = new McpHandler { Mode = "timeout" };
        var service = CreateService(server, new ScriptedModel("web_search"));
        using var cancellation = new CancellationTokenSource();
        var run = Run(service, new Apps { EnableWebSearch = true }, new ChatHistory(), cancellationToken: cancellation.Token);
        await server.ToolStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
        Assert.True(server.ToolCancelled);
    }

    [Fact]
    public async Task InvalidFetchUrlNeverReachesMcp()
    {
        using var server = new McpHandler();
        var model = new ScriptedModel("invalid_fetch");
        await Run(CreateService(server, model), new Apps { EnableWebSearch = true }, new ChatHistory());
        Assert.Contains("HTTP(S)", Assert.Single(model.ConsumedResults));
        Assert.Empty(server.Requests);
    }

    // Scripted model, not model-quality evaluation: the real ChatService, SK dispatcher,
    // official MCP transport and downstream tool-message consumption all execute.
    private static ChatService CreateService(McpHandler server, ScriptedModel model, TimeSpan? timeout = null) =>
        new(new TestKernelService(model), null!, null!, null!, new TestHttpFactory(server, timeout));

    private static async Task<string> Run(ChatService service, Apps app, ChatHistory history,
        WebSearchConversation? conversation = null, CancellationToken cancellationToken = default)
    {
        var result = new StringBuilder();
        await foreach (var text in service.SendChatByAppAsync(app, history, conversation, cancellationToken)) result.Append(text);
        return result.ToString();
    }

    private sealed class TestHttpFactory(McpHandler handler, TimeSpan? timeout) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            Assert.Equal("ParallelSearch", name);
            return new(handler, disposeHandler: false) { Timeout = timeout ?? TimeSpan.FromSeconds(10) };
        }
    }

    private sealed class TestKernelService(ScriptedModel model) : IKernelService
    {
        public Kernel GetKernelByApp(Apps app)
        {
            var builder = Kernel.CreateBuilder();
            builder.Services.AddSingleton<IChatCompletionService>(model);
            return builder.Build();
        }
        public Kernel GetKernelByAIModelID(string modelid) => throw new NotSupportedException();
        public void ImportFunctionsByApp(Apps app, Kernel kernel) =>
            kernel.ImportPluginFromFunctions("AntSKFunctions", new[] { KernelFunctionFactory.CreateFromMethod(() => "custom result", "custom") });
        public Task<string> HistorySummarize(Kernel kernel, string questions, string history) => throw new NotSupportedException();
    }

    private sealed class ScriptedModel(params string[] calls) : IChatCompletionService
    {
        private readonly Queue<string> _calls = new(calls);
        public List<string> ConsumedResults { get; } = new();
        public string[] PluginNames { get; private set; } = [];
        public IReadOnlyDictionary<string, object?> Attributes { get; } = new Dictionary<string, object?>();
        public void Enqueue(string call) => _calls.Enqueue(call);
        public Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(ChatHistory history,
            PromptExecutionSettings? settings = null, Kernel? kernel = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PluginNames = kernel!.Plugins.Select(p => p.Name).ToArray();
            if (history.LastOrDefault()?.Role == AuthorRole.Tool)
                ConsumedResults.Add(history.Last().Items.OfType<FunctionResultContent>().Single().Result?.ToString() ?? "");
            var response = new ChatMessageContent(AuthorRole.Assistant, content: null);
            if (_calls.TryDequeue(out var call))
            {
                var args = call switch
                {
                    "web_search" => new KernelArguments { ["objective"] = "Find a source", ["search_queries"] = new[] { "official source current information" } },
                    "web_fetch" => new KernelArguments { ["urls"] = new[] { "https://example.org/source" } },
                    "invalid_fetch" => new KernelArguments { ["urls"] = new[] { "file:///etc/passwd" } },
                    _ => new KernelArguments()
                };
                response.Items.Add(new FunctionCallContent(call == "invalid_fetch" ? "web_fetch" : call,
                    call == "custom" ? "AntSKFunctions" : "ParallelSearch", Guid.NewGuid().ToString(), args));
            }
            else response.Content = "Answer with https://example.org/source";
            return Task.FromResult<IReadOnlyList<ChatMessageContent>>(new[] { response });
        }
        public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(ChatHistory history,
            PromptExecutionSettings? settings = null, Kernel? kernel = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            Assert.DoesNotContain(kernel!.Plugins, p => p.Name == "ParallelSearch");
            yield return new(AuthorRole.Assistant, "streamed answer");
        }
    }

    private sealed record RecordedRequest(string Method, string Url, string? UserAgent, string? Authorization);
    private sealed record RecordedCall(string Name, string SessionId, JsonElement Arguments);

    private sealed class McpHandler : HttpMessageHandler
    {
        public ConcurrentQueue<RecordedRequest> Requests { get; } = new();
        public ConcurrentQueue<RecordedCall> Calls { get; } = new();
        public string Mode { get; set; } = "success";
        public TaskCompletionSource ToolStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool ToolCancelled { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Method != HttpMethod.Post) return new(HttpStatusCode.MethodNotAllowed);
            var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken)).RootElement;
            var method = json.GetProperty("method").GetString()!;
            Requests.Enqueue(new(method, request.RequestUri!.ToString(), request.Headers.UserAgent.ToString(), request.Headers.Authorization?.ToString()));
            if (!json.TryGetProperty("id", out var id)) return new(HttpStatusCode.Accepted);
            object result;
            switch (method)
            {
                case "initialize":
                    result = new { protocolVersion = json.GetProperty("params").GetProperty("protocolVersion").GetString(), capabilities = new { tools = new { } }, serverInfo = new { name = "deterministic-test", version = "1.0" } };
                    break;
                case "tools/list":
                    result = new { tools = new[] { Tool("web_search"), Tool("web_fetch") } };
                    break;
                case "tools/call":
                    var parameters = json.GetProperty("params");
                    var args = parameters.GetProperty("arguments");
                    Calls.Enqueue(new(parameters.GetProperty("name").GetString()!, args.GetProperty("session_id").GetString()!, args.Clone()));
                    ToolStarted.TrySetResult();
                    if (Mode == "http") return new(HttpStatusCode.ServiceUnavailable);
                    if (Mode == "timeout")
                    {
                        try { await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken); }
                        catch (OperationCanceledException) { ToolCancelled = true; throw; }
                    }
                    if (Mode == "rpc") return Response(new { jsonrpc = "2.0", id, error = new { code = -32603, message = "test protocol failure" } });
                    result = new { isError = Mode == "quota", content = new[] { new { type = "text", text = Mode == "quota" ? "anonymous quota exceeded" : "source excerpt https://example.org/source" } } };
                    break;
                default: throw new InvalidOperationException("Unexpected MCP method: " + method);
            }
            return Response(new { jsonrpc = "2.0", id, result });
        }
        private static object Tool(string name) => new { name, description = name, inputSchema = new { type = "object", properties = new { } } };
        private static HttpResponseMessage Response(object value) => new(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json")
        };
    }
}
