using Microsoft.Extensions.AI;
using System.Text.Json.Serialization;

namespace ChatBot.BlazorServerOnly.Models;

public class ConversationMessage
{
    public required ChatMessage RawMessage { get; init; }
    public UsageDetails? Usage { get; set; }

    [JsonIgnore]
    public ChatRole Role => RawMessage.Role;

    [JsonIgnore]
    public string Text => RawMessage.Text;

    [JsonIgnore]
    public IList<AIContent> Contents => RawMessage.Contents;

    public string? ImagePath { get; init; }
}