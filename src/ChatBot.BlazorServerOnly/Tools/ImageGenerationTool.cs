using AgentFrameworkToolkit.AzureOpenAI;
using Azure.AI.OpenAI;
using ChatBot.BlazorServerOnly.Models;
using Microsoft.Extensions.AI;
using OpenAI.Images;
using System.ClientModel;

namespace ChatBot.BlazorServerOnly.Tools;

public class ImageGenerationTool(AzureOpenAIAgentFactory azureOpenAIAgentFactory, Conversation conversation)
{
    public async Task<string> GenerateImageAsync(string prompt)
    {
        AzureOpenAIClient client = azureOpenAIAgentFactory.Connection.GetClient();
        ImageClient imageClient = client.GetImageClient("gpt-image-1");
        ClientResult<GeneratedImage> image = await imageClient.GenerateImageAsync(prompt);

        string generatedImagesFolder = "generated-images";
        string directory = Path.Combine(
            Environment.CurrentDirectory,
            "wwwroot",
            generatedImagesFolder);

        Directory.CreateDirectory(directory);

        string fileName = $"{Guid.CreateVersion7()}.png";
        string path = Path.Combine(directory, fileName);

        await using (FileStream fileStream = File.Create(path))
        {
            await image.Value.ImageBytes.ToStream().CopyToAsync(fileStream);
        }
        
        conversation.Messages.Add(new ConversationMessage
        {
            ImagePath = $"{generatedImagesFolder}/{fileName}",
            Role = ChatRole.Assistant
        });

        return "Image Generated";
    }
}
