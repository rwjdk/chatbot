using Microsoft.Extensions.AI;

namespace ChatBot.BlazorServerOnly.Models;

public class ConversationMessage
{
    public required ChatMessage RawMessage { get; init; }
    public UsageDetails? Usage { get; set; }
    public ChatRole Role => RawMessage.Role;
    public string Text => RawMessage.Text;
    public IList<AIContent> Contents => RawMessage.Contents;
}