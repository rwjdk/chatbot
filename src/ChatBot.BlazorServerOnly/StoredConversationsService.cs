using System.Text.Json;
using ChatBot.BlazorServerOnly.Models;

namespace ChatBot.BlazorServerOnly;

public class StoredConversationsService
{
    public async Task<List<Conversation>> LoadPreviousConversationsAsync()
    {
        List<Conversation> conversations = [];
        string conversationFolder = GetConversationFolder();
        foreach (string conversationFile in Directory.GetFiles(conversationFolder, "*.json"))
        {
            string json = await File.ReadAllTextAsync(conversationFile);
            conversations.Add(JsonSerializer.Deserialize<Conversation>(json)!);
        }
        return conversations;
    }

    public async Task StoreSessionAsync(Conversation conversation)
    {
        string conversationFolder = GetConversationFolder();
        string conversationFile = Path.Combine(conversationFolder, $"{conversation.Id}.json");
        await File.WriteAllTextAsync(conversationFile, JsonSerializer.Serialize(conversation));
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