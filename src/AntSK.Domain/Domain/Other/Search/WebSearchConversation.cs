using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel.ChatCompletion;

namespace AntSK.Domain.Domain.Other.Search;

/// <summary>Opaque identity owned by a conversation, separate from MCP connections.</summary>
public sealed record WebSearchConversation(Guid Id)
{
    private static readonly ConditionalWeakTable<ChatHistory, WebSearchConversation> Histories = new();

    public WebSearchConversation() : this(Guid.NewGuid()) { }

    public static WebSearchConversation ForHistory(ChatHistory history) =>
        Histories.GetValue(history, _ => new WebSearchConversation());
}
