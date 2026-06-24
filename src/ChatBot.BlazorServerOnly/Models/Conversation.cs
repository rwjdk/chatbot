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
}