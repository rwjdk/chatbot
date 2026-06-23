using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ChatBot.BlazorServerOnly.Extensions;

public static class AgentSessionExtensions
{
    public static async Task<bool> GenerateTitleForSessionIfNeededAsync(this AgentSession session, AIAgent titleGenerationAgent, string input)
    {
        session.TryGetInMemoryChatHistory(out List<ChatMessage>? currentMessages);
        if (currentMessages == null)
        {
            //This is first Message in a conversation - Lets generate a conversation-title
            AgentResponse<string> response = await titleGenerationAgent.RunAsync<string>($"Given the following message: '{input}' generate a max 25 char long title for this question");
            session.StateBag.SetValue("title", response.Result);
            return true;
        }

        return false;
    }

    public static string GetOrGenerateId(this AgentSession session)
    {
        string? id = session.StateBag.GetValue<string>("id");
        if (id != null)
        {
            return id;
        }

        id = Guid.CreateVersion7().ToString();
        session.StateBag.SetValue("id", id);

        return id;
    }
}