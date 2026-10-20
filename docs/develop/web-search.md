# 会话应用联网搜索 / Chat web search

新建会话应用默认开启「联网搜索」。模型可以通过 [Parallel Search MCP](https://docs.parallel.ai/integrations/mcp/search-mcp)
搜索公开网页和读取网页内容，并在回答中引用来源 URL。匿名搜索使用 Fast 模式，无需账户或 API Key。

在「应用」→「新增/编辑」→「会话应用」中关闭「联网搜索」即可禁用这两个工具。
已有应用保持关闭，编辑应用后可以手动开启；原有 API 插件、原生插件、模型配置及知识库检索不变。
数据库新增可空的 `EnableWebSearch` 字段；已有记录的空值表示关闭。

搜索问题和需要读取的 URL 会发送到 `https://search.parallel.ai/mcp`。外部网页是不可信数据，
不应当作系统指令。处理敏感数据或离线部署时，请在保存新应用前关闭联网搜索。
该功能只用于会话应用，不会把知识库检索自动转为网络搜索。模型没有调用网页工具时不会建立 MCP 连接。

服务有匿名用量限制。网络错误、服务限流和超时会作为工具结果交给模型，不会自动换用其他搜索服务或身份重试。
单次工具请求限时 30 秒，每轮会话最多进行 10 轮工具调用。需要支持函数调用的会话模型。

浏览器会话保存独立的随机搜索标识，同一会话的搜索、读取和后续提问共用该标识；清空聊天记录时创建新标识。
OpenAI 兼容 API 的流式和非流式请求使用相同工具流程。每个独立 API 请求是一次研究运行，
其内部所有网页调用共用一个随机标识；API 不把客户端消息内容或用户信息当作搜索标识。

New chat applications enable web search by default. Turn **联网搜索** off in the application editor to disable
both web tools. Existing applications remain disabled until explicitly enabled. Parallel provides anonymous web
search in Fast mode and page extraction without an account or API key. Existing custom tools and knowledge-base
retrieval remain available with their saved settings.

Search questions and fetched URLs leave your deployment and go to Parallel. Disable this setting before saving
an application intended for offline use or sensitive data. A tool-capable model decides when to search, and source
URLs are returned with the excerpts. Network errors, timeouts and usage limits are returned to the model without
silently switching providers. Each stateless API request owns a new research-run identity; browser conversations
reuse their stored identity until chat history is cleared.

## 开发验证 / Developer checks

With the .NET 9 SDK installed, from the repository root:

```sh
dotnet build src/AntSK/AntSK.csproj
dotnet test src/AntSK.Search.Tests/AntSK.Search.Tests.csproj
```

The deterministic tests drive `ChatService` with a scripted chat model and a recorded HTTP MCP server.
They exercise Semantic Kernel tool dispatch and downstream result consumption, including disabled applications,
custom tools, conversation isolation, errors, cancellation and timeouts. They do not evaluate model answer quality.
