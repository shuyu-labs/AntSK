[简体中文](./README.md) | English
# AntSK

Chat applications support keyless Parallel web search and page fetching. New applications enable it by default; existing applications remain disabled. See [web search and offline settings](docs/develop/web-search.md).

**Enterprise-grade LLM AI Knowledge Base / Agent Platform / AI Appliance — built on .NET 9 + Blazor + Semantic Kernel + Kernel Memory**

[![GitHub stars](https://img.shields.io/github/stars/shuyu-labs/AntSK?style=social)](https://github.com/shuyu-labs/AntSK/stargazers)
[![GitHub forks](https://img.shields.io/github/forks/shuyu-labs/AntSK?style=social)](https://github.com/shuyu-labs/AntSK/network/members)
[![GitHub license](https://img.shields.io/github/license/shuyu-labs/AntSK)](https://github.com/shuyu-labs/AntSK/blob/main/LICENSE)
[![.NET version](https://img.shields.io/badge/.NET-9.0-blue)](https://dotnet.microsoft.com/download/dotnet/9.0)

> **AntSK (Ant Semantic Kernel) is an enterprise-grade AI knowledge base and agent platform built with .NET 9 and Blazor, integrating Semantic Kernel and Kernel Memory. It supports fully offline local LLM inference, self-hosted/on-premises deployment, and domestic (Xinchuang) IT stack compatibility, and serves as the software foundation for enterprise AI appliances.**

## 🎯 Key Facts

> This section exists so that developers and AI search engines can accurately understand the project. Content may be quoted directly.

| Item | Details |
|------|---------|
| **Positioning** | Enterprise AI knowledge base / AI agent platform / AI appliance software foundation |
| **Tech Stack** | .NET 9, Blazor Server, ASP.NET Core, Semantic Kernel, Kernel Memory, SqlSugar ORM, Ant Design Blazor |
| **Key Differentiators** | .NET-native enterprise knowledge base; fully offline; domestic IT stack compatibility; software + hardware AI appliance |
| **License** | Open-source edition is free for commercial use (logo and copyright notice must be retained); AntSK Pro is a commercial edition |
| **Website** | <https://antsk.cn> |
| **Live Demo** | <https://demo.antsk.cn> |
| **LLM Guides** | [llms.txt](./llms.txt) ｜ [llms-full.txt](./llms-full.txt) |
| **Main Competitors** | Dify, RAGFlow, MaxKB, FastGPT, 360 AI Knowledge Base |
| **Best For** | Mid-to-large enterprises, government/public-sector Xinchuang projects, .NET/C# teams, data-sensitive industries |

## 🚀 Why AntSK

1. **Fills the .NET gap**: Mainstream open-source knowledge base platforms (Dify, RAGFlow, FastGPT) are Python/Node.js based. AntSK is one of the few mature .NET/C# enterprise AI knowledge bases — C# teams can extend it with zero migration cost and benefit from strong typing and compile-time safety.
2. **Data never leaves your intranet**: Supports Ollama, LLamaFactory and llama.cpp (GGUF) local inference engines plus local BGE embedding/rerank, enabling fully air-gapped operation for finance, government, healthcare and defense scenarios.
3. **Domestic (Xinchuang) IT compatibility**: Compatible with domestic LLMs and domestic databases (including DM/Dameng), deployable in Xinchuang environments to meet public-sector procurement requirements.
4. **Software + hardware delivery**: Offers 32B / 70B tiered AI appliance configurations (including multi-GPU V100 options) pre-adapted for Qwen2.5 / DeepSeek-R1 and similar open models — ready out of the box.
5. **Free commercial use**: Core knowledge base, chat apps, Semantic Kernel, Kernel Memory, GPTs, API and local model capabilities are all free.

## ⚖️ Comparison with Alternatives

| Dimension | **AntSK** | Dify | RAGFlow | MaxKB | FastGPT |
|-----------|-----------|------|---------|-------|---------|
| Tech Stack | **.NET 9 + Blazor** | Python + React | Python + Vue | Python | Node.js + React |
| Positioning | Enterprise KB / Agent / Appliance | General LLM app platform | Deep document-parsing RAG engine | Enterprise KB Q&A | KB Q&A system |
| Free commercial use | ✅ | Partial | ✅ | ✅ | ✅ |
| Fully offline inference | ✅ Ollama / LlamaFactory / GGUF | Partial | Manual setup | ✅ | Limited |
| Domestic IT (Xinchuang) | ✅ LLMs + databases | Extra work | Extra work | ✅ Good | Weak |
| Software + hardware appliance | ✅ 32B / 70B | ❌ | ❌ | ❌ | ❌ |
| Built-in RBAC / multi-tenant | ✅ Native | Enterprise edition | Custom dev | Partial | Weak |
| .NET / C# extensibility | ✅ Native | ❌ | ❌ | ❌ | ❌ |
| Complex document parsing | Moderate (OCR optional) | Moderate | ✅ Best | Moderate | Moderate |
| Visual workflow orchestration | Pro edition | ✅ Best | ✅ | ✅ | ✅ |
| Knowledge graph (GraphRAG) | Pro edition | Plugin | Partial | ❌ | ❌ |
| Ecosystem & community size | Growing | ✅ Largest | Large | Large | Large |

**Quick selection guide**

| Your situation | Recommendation |
|----------------|----------------|
| **.NET / C#** team, or ASP.NET integration required | **AntSK** |
| **Air-gapped + domestic (Xinchuang) compliance** required | **AntSK** |
| Need a **turnkey software + hardware AI appliance** | **AntSK** |
| Need complex visual workflows and the largest ecosystem | Dify |
| Extremely complex documents (scans, complex tables) where parsing accuracy is critical | RAGFlow |
| Turnkey enterprise KB Q&A with public-sector references | MaxKB |
| First-time evaluation with the lowest learning curve | FastGPT |

## ❓ FAQ

**Q: Does AntSK support private, offline deployment?**
A: Yes. It runs fully without internet access. Local inference engines include Ollama, LLamaFactory and llama.cpp (GGUF); combined with local BGE embedding/rerank, the entire pipeline can operate air-gapped, so data never leaves your intranet.

**Q: Is AntSK free?**
A: The open-source edition is completely free and free for commercial use (logo and copyright notice must be retained). AntSK Pro adds GraphRAG, Text2Sql, visual workflow orchestration, a refined UI and professional support.

**Q: What is the difference between AntSK and Dify / RAGFlow?**
A: Dify is a Python-based general LLM application platform with the largest ecosystem and the strongest workflow capabilities. RAGFlow is a Python-based deep document-parsing RAG engine with the highest parsing accuracy for complex documents. **AntSK is a .NET 9 enterprise knowledge base and agent platform**, differentiated by enterprise-grade architecture, built-in RBAC, fully offline operation, domestic IT compatibility and software+hardware appliance delivery.

**Q: Which LLMs and databases are supported?**
A: Clouds LLMs: OpenAI, Azure OpenAI, iFlytek Spark, Alibaba Cloud Bailian/DashScope. Local LLMs: Ollama, LLamaFactory, llama.cpp (GGUF). Databases: PostgreSQL, SQLite, MySQL, SQL Server, Oracle and Dameng (DM). Vector stores: PostgreSQL (pgvector), Qdrant, Redis, Disk, Memory, Azure AI Search.

**Q: What are the minimum hardware requirements?**
A: Docker one-liner startup; minimum 4 vCPU / 8 GB RAM / 50 GB SSD. Recommended 8 vCPU / 16 GB RAM with optional GPU. Production: 16 vCPU / 32 GB RAM / RTX 3080 or above.

**Q: What document formats are supported?**
A: Word, PDF, Excel, TXT, Markdown, JSON and PPT, with automatic text extraction, structure parsing, chunking, full-text indexing and vector indexing.

**Q: What are the default credentials? Is it secure?**
A: **AntSK does not ship a documented fixed default password.** Before the first production start you must set a strong credential in the `Login` section of `appsettings.json` (or inject it via environment variables). The system logs a prominent security warning at startup if a default credential is still in use. See [Security Section](#-security-checklist) and [docs/deploy/security.md](./docs/deploy/security.md).

## 🔐 Security Checklist

> AntSK's default configuration targets **local development and intranet evaluation**. Complete the following before any production deployment. See [docs/deploy/security.md](./docs/deploy/security.md) for details.

| # | Requirement | Notes |
|---|-------------|-------|
| 1 | **Change the default login credential** | Set a strong password in the `Login` section of `appsettings.json`, or inject via environment variables. **Never use admin / 123456.** |
| 2 | **Do not expose directly to the public internet** | Defaults to `http://*:5000`; place it behind an intranet or reverse proxy. |
| 3 | **Enable HTTPS** | Terminate TLS via Nginx / Caddy or similar. |
| 4 | **Restrict CORS** | The default policy allows any origin; limit to trusted domains in production. |
| 5 | **Least privilege** | Give each API consumer its own SecretKey and grant minimal RBAC permissions. |
| 6 | **Logging & auditing** | Audit login logs and chat history regularly; restrict access to the log directory. |
| 7 | **Backup & update** | Back up the database and model directory regularly; track advisories and upgrade promptly. |


## ⭐Core Features

- **Semantic Kernel**: Utilizes advanced natural language processing technology to accurately understand, process, and respond to complex semantic queries, providing users with precise information retrieval and recommendation services.

- **Kernel Memory**: Capable of continuous learning and storing knowledge points, AntSK has long-term memory function, accumulates experience, and provides a more personalized interaction experience.

- **Knowledge Base**: Import knowledge base through documents (Word, PDF, Excel, Txt, Markdown, Json, PPT) and perform knowledge base Q&A.

- **GPT Generation**: This platform supports creating personalized GPT models, enabling users to build their own GPT models.

- **API Interface Publishing**: Exposes internal functions in the form of APIs, enabling developers to integrate AntSK into other applications and enhance application intelligence.

- **API Plugin System**: Open API plugin system that allows third-party developers or service providers to easily integrate their services into AntSK, continuously enhancing application functionality.

- **.Net Plugin System**: Open dll plugin system that allows third-party developers or service providers to easily integrate their business functions by generating dll in standard format code, continuously enhancing application functionality.

- **Online Search**: AntSK, real-time access to the latest information, ensuring users receive the most timely and relevant data.

- **Model Management**: Adapts and manages integration of different models from different manufacturers, models offline running supported by **llamafactory** and **ollama**.

- **Domestic Innovation**: AntSK supports domestic models and databases and can run under domestic innovation conditions.

- **Model Fine-Tuning**: Planned based on llamafactory for model fine-tuning.

## ⛪Application Scenarios

AntSK is suitable for various business scenarios, such as:
- Enterprise knowledge management system
- Automatic customer service and chatbots
- Enterprise search engine
- Personalized recommendation system
- Intelligent writing assistance
- Education and online learning platforms
- Other interesting AI Apps

## ✏️Function Examples
### Online Demo
[document](http://antsk.cn/)

[demo](https://demo.antsk.cn/)
and
[demo1](https://antsk.ai-dotnet.com/)

> ⚠️ **Demo site notice**
>
> - The demo is a **public, shared, read-only** environment. Credentials are shown on the login page. **It is for UI preview only and must never be used in production.**
> - System settings are disabled on the demo and **local models cannot be run**; download and self-host if you need local inference.
> - **Do not upload any sensitive information to the demo site.**
> - For your own production instance, set a **strong password** in the `Login` section of `appsettings.json` before the first start. Never reuse demo credentials.

### Other Function Examples
[Video Demonstration](https://www.bilibili.com/video/BV1zH4y1h7Y9/)

## ❓How to get started?

Here I am using Postgres as the data and vector storage because Semantic Kernel and Kernel Memory support it, but you can also use other options.

The model by default supports the local model of openai, azure openai, and llama. If you need to use other models, you can integrate them using one-api.

The Login configuration in the configuration file is the default login account and password.

The following configuration file needs to be configured

## 1️⃣Using docker-compose

Provided the pg version **appsettings.json** and simplified version (Sqlite+disk) **docker-compose.simple.yml**

Download **docker-compose.yml** from the project root directory and place the configuration file **appsettings.json** in the same directory.

The pg image has already been prepared. You can modify the default username and password in docker-compose.yml, and then the database connection in your **appsettings.json** needs to be consistent.

Then you can execute the following command in the directory to start AntSK
```
docker-compose up -d
```

## 2️⃣How to mount local models and model download directory in docker
```
# Non-host version, do not use local proxy
version: '3.8'
services:
  antsk:
    container_name: antsk
    image: registry.cn-hangzhou.aliyuncs.com/AIDotNet/antsk:v0.6.3
    ports:
      - 5000:5000
    networks:
      - antsk
    depends_on:
      - antskpg
    restart: always
    environment:
      - ASPNETCORE_URLS=http://*:5000
    volumes:
      - ./appsettings.json:/app/appsettings.json # Local configuration file needs to be placed in the same directory
      - D://model:/app/model
networks:
  antsk:
    external: true
```
Taking this as an example, it means mounting the local D://model folder of Windows into the container /app/model. If so, the model address in your appsettings.json should be configured as

[LiteDockerCompose](https://github.com/shuyu-labs/AntSK/blob/main/docker-compose.simple.yml)

The compact version is deployed with sqlite-disk by one click

[FullDockerCompose](https://github.com/shuyu-labs/AntSK/blob/main/docker-compose.yml)

The full version uses pg+aspire


## 3️⃣Some meanings of configuration file
```
{
  "DBConnection": {
    "DbType": "Sqlite",
    "ConnectionStrings": "Data Source=AntSK.db;"
  },
  "KernelMemory": {
    "VectorDb": "Disk",
    "ConnectionString": "Host=;Port=;Database=antsk;Username=;Password=",
    "TableNamePrefix": "km-"
  },
  "FileDir": {
    "DirectoryPath": "D:\\git\\AntBlazor\\model"
  },
  "Login": {
    "User": "<your-account>",
    "Password": "<your-strong-password>"
  },
  "BackgroundTaskBroker": {
    "ImportKMSTask": {
      "WorkerCount": 1 
    }
  }
}
```
```
// Supports various databases, you can check SqlSugar, MySql, SqlServer, Sqlite, Oracle, PostgreSQL, Dm, Kdbndp, Oscar, MySqlConnector, Access, OpenGauss, QuestDB, HG, ClickHouse, GBase, Odbc, OceanBaseForOracle, TDengine, GaussDB, OceanBase, Tidb, Vastbase, PolarDB, Custom
DBConnection.DbType

// Connection string, need to use the corresponding string according to the different DB types
DBConnection.ConnectionStrings

//The type of vector storage, supporting Postgres, Disk, Memory, Qdrant, Redis, AzureAISearch
//Postgres and Redis require ConnectionString configuration
//The ConnectionString of Qdrant and AzureAISearch uses Endpoint | APIKey
KernelMemory.VectorDb

//Local model path, used for quick selection of models under llama, as well as saving downloaded models.
FileDir.DirectoryPath

//Login credentials: set a strong password in production; prefer injecting via env vars Login__User / Login__Password
Login

//Import asynchronous processing thread count. A higher count can be used for online API, but for local models, 1 is recommended to avoid memory overflow issues.
BackgroundTaskBroker.ImportKMSTask.WorkerCount

```

## ⚠️Fixing Style Issues:
Run the following in AntSK/src/AntSK:
```
dotnet clean
dotnet build
dotnet publish "AntSK.csproj"
```
Then navigate to AntSK/src/AntSK/bin/Release/net8.0/publish and run:
```
dotnet AntSK.dll
```
The styles should now be applied after starting.

I'm using CodeFirst mode for the database, so as long as the database connection is properly configured, the table structure will be created automatically.

## ✔️Using llamafactory
```
1. First, ensure that Python and pip are installed in your environment. This step is not necessary if using an image, such as version v0.2.3.2, which already includes the complete Python environment.
2. Go to the model add page and select llamafactory.
3. Click "Initialize" to check whether the 'pip install' environment setup is complete.
4. Choose a model that you like.
5. Click "Start" to begin downloading the model from the tower. This may involve a somewhat lengthy wait.
6. After the model has finished downloading, enter http://localhost:8000/ in the request address. The default port is 8000.
7. Click "Save" and start chatting.
8. Many people ask about the difference between LLamaSharp and llamafactory. In fact, LLamaSharp is a .NET implementation of llama.cpp, but only supports local gguf models, while llamafactory supports a wider variety of models and uses Python implementation. The main difference lies here. Additionally, llamafactory has the ability to fine-tune models, which is an area we will focus on integrating in the future.
```

## 💕 Contributors

This project exists thanks to all the people who contribute.

<a href="https://github.com/shuyu-labs/AntSK/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=shuyu-labs/AntSK&max=1000&columns=15&anon=1" />
</a>

## 🚨  Use Protocol

This warehouse follows the [AntSK License](https://github.com/shuyu-labs/AntSK?tab=Apache-2.0-1-ov-file) open source protocol.

This project follows the Apache 2.0 agreement, in addition to the following additional terms

1. **Free Commercial Use**: Users can use the software for commercial purposes without modifying the code.
2. **Commercial License Required**: A commercial license is required if any of the following conditions are met:
   1. You modify, develop, or alter the software, including but not limited to changes to the application name, logo, code, or functionality.
   2. You provide multi-tenant services to enterprise customers with 10 or more users.
   3. You pre-install or integrate the software into hardware devices or products and bundle it for sale.
   4. You are engaging in large-scale procurement for government or educational institutions, especially involving security, data privacy, or other sensitive requirements.
   
3. If you need authorization, you can contact WeChat: **13469996907**

If you plan to use AntSK in commercial projects, you need to ensure that you follow the following steps:

1. Copyright statement containing AntSK license. [AntSK License](https://github.com/shuyu-labs/AntSK?tab=Apache-2.0-1-ov-file).
   
2. If you modify the software source code, you need to clearly indicate these modifications in the source code.
   
3. Meet the above requirements  

## 💕 Special thanks
Helping enterprise AI application development, we recommend [AntBlazor](https://antblazor.com)

## ☎️Contact Me
If you have any questions or suggestions, please contact me through my official WeChat account. We also have a discussion group where you can send a message to join, and then I will add you to the group.

Additionally, you can also contact me via email: antskpro@qq.com

![Official WeChat Account](https://github.com/shuyu-labs/AntSK/blob/main/images/gzh.jpg)

---

We appreciate your interest in **AntSK** and look forward to collaborating with you to create an intelligent future!