using System.Text.Json;
using ChatBot.BlazorServerOnly.Models;

namespace ChatBot.BlazorServerOnly.Services;

public class UserPersonalizationService
{
    public UserPersonalization? GetPersonalization(string userId)
    {
        string folder = GetUserPersonalizationFolder();

        string path = Path.Combine(folder, $"{userId}.json");
        if (!File.Exists(path))
        {
            return null;
        }
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<UserPersonalization>(json);
    }

    private static string GetUserPersonalizationFolder()
    {
        string tempPath = Path.GetTempPath();
        string conversationFolder = Path.Combine(tempPath, "blazor-server-user-personalization");
        if (!Directory.Exists(conversationFolder))
        {
            Directory.CreateDirectory(conversationFolder);
        }
        return conversationFolder;
    }

    public void SavePersonalization(string userId, UserPersonalization personalization)
    {
        string folder = GetUserPersonalizationFolder();
        string path = Path.Combine(folder, $"{userId}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(personalization));
    }
}