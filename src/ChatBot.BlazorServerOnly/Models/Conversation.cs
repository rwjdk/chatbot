using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ChatBot.BlazorServerOnly.Models;

public class Conversation
{
    public static Conversation NewConversation()
    {
        return new Conversation
        {
            Id = Guid.CreateVersion7()
        };
    }

    public List<ConversationMessage> Messages { get; init; } = [];

    public required Guid Id { get; init; }
    public string? Title { get; set; }

    [JsonIgnore]
    public bool MissingATitle => string.IsNullOrWhiteSpace(Title);

    public void AddUserMessage(string message)
    {
        Messages.Add(new ConversationMessage
        {
            RawMessage = new ChatMessage(ChatRole.User, message)
        });
    }

    public List<ChatMessage> GetRawMessages()
    {
        return Messages.Select(x => x.RawMessage).ToList();
    }

    public void AddDataFromAgentResponse(AgentResponse response)
    {
        foreach (ChatMessage message in response.Messages)
        {
            Messages.Add(new ConversationMessage
            {
                RawMessage = message,
            });
        }
        Messages.LastOrDefault()?.Usage = response.Usage;
    }

    public string GetAsImageGenerationPrompt()
    {
        StringBuilder prompt = new();
        foreach (ConversationMessage message in Messages)
        {
            prompt.AppendLine($"<message role=\"{message.Role}\">{message.Text}</message>");
        }

        return prompt.ToString();
    }
}