using ChatBot.BlazorServerOnly.Models;
using Microsoft.Extensions.AI;

namespace ChatBot.BlazorServerOnly.Services;

public class FileUploadStorageService(IWebHostEnvironment webHostEnvironment)
{
    public async Task<ConversationAttachment> SaveAsync(string fileName, string contentType, byte[] bytes)
    {
        string uploadFolder = GetUploadFolder();
        Directory.CreateDirectory(uploadFolder);

        string storedFileName = $"{Guid.CreateVersion7()}{Path.GetExtension(fileName)}";
        string filePath = Path.Combine(uploadFolder, storedFileName);
        await File.WriteAllBytesAsync(filePath, bytes);

        return new ConversationAttachment
        {
            FileName = fileName,
            StoredFileName = storedFileName,
            ContentType = contentType,
            RelativePath = $"/attachments/{storedFileName}"
        };
    }

    public async Task<DataContent> CreateDataContentAsync(ConversationAttachment attachment)
    {
        string filePath = GetRequiredFilePath(attachment.StoredFileName);
        byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
        string dataUri = $"data:{attachment.ContentType};base64,{Convert.ToBase64String(fileBytes)}";
        return new DataContent(dataUri, attachment.ContentType);
    }

    public string? GetFilePath(string storedFileName)
    {
        string safeFileName = Path.GetFileName(storedFileName);
        string filePath = Path.Combine(GetUploadFolder(), safeFileName);
        if (!File.Exists(filePath))
        {
            return null;
        }

        return filePath;
    }

    private string GetRequiredFilePath(string storedFileName)
    {
        string? filePath = GetFilePath(storedFileName);
        if (filePath is null)
        {
            throw new FileNotFoundException("Uploaded file was not found.", storedFileName);
        }

        return filePath;
    }

    private string GetUploadFolder()
    {
        return Path.Combine(webHostEnvironment.ContentRootPath, "App_Data", "file-uploads");
    }
}
