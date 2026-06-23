using System.Text.Json;
using ChatBot.BlazorServerOnly.Extensions;
using Microsoft.Agents.AI;

namespace ChatBot.BlazorServerOnly;

public class StoredSessionsService
{
    public async Task<List<AgentSession>> LoadPreviousSessionsAsync(AIAgent agent)
    {
        List<AgentSession> sessions = [];
        string conversationFolder = GetConversationFolder();
        foreach (string conversationFile in Directory.GetFiles(conversationFolder, "*.json"))
        {
            JsonElement jsonElement = JsonElement.Parse(await File.ReadAllTextAsync(conversationFile));
            AgentSession session = await agent.DeserializeSessionAsync(jsonElement);
            sessions.Add(session);
        }
        return sessions;
    }

    public async Task StoreSessionAsync(AIAgent agent, AgentSession session)
    {
        string conversationFolder = GetConversationFolder();
        string id = session.GetOrGenerateId();
        string conversationFile = Path.Combine(conversationFolder, $"{id}.json");
        JsonElement serializedSession = await agent.SerializeSessionAsync(session);
        await File.WriteAllTextAsync(conversationFile, serializedSession.GetRawText());
    }

    private static string GetConversationFolder()
    {
        string tempPath = Path.GetTempPath();
        string conversationFolder = Path.Combine(tempPath, "blazor-server-only-conversations");
        if (!Directory.Exists(conversationFolder))
        {
            Directory.CreateDirectory(conversationFolder);
        }
        return conversationFolder;
    }

}