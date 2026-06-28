namespace ChatBot.BlazorServerOnly.Models;

public class ConversationAttachment
{
    public required string FileName { get; init; }
    public required string StoredFileName { get; init; }
    public required string ContentType { get; init; }
    public required string RelativePath { get; init; }
}
