using System.Security.Cryptography;
using System.Text;
using ChatBot.BlazorServerOnly.Models;
using Microsoft.Extensions.AI;

namespace ChatBot.BlazorServerOnly.Services;

public class FileUploadStorageService(IWebHostEnvironment webHostEnvironment)
{
    public async Task<ConversationAttachment> SaveAsync(string userId, string fileName, string contentType, byte[] bytes)
    {
        string uploadFolder = GetUploadFolder(userId);
        Directory.CreateDirectory(uploadFolder);

        string storedFileName = $"{Guid.CreateVersion7()}{Path.GetExtension(fileName)}";
        string filePath = Path.Combine(uploadFolder, storedFileName);
        await File.WriteAllBytesAsync(filePath, bytes);

        return new ConversationAttachment
        {
            UserId = userId,
            FileName = fileName,
            StoredFileName = storedFileName,
            ContentType = contentType,
            RelativePath = $"/attachments/{storedFileName}"
        };
    }

    public async Task<DataContent> CreateDataContentAsync(ConversationAttachment attachment)
    {
        string filePath = GetRequiredFilePath(attachment.UserId, attachment.StoredFileName);
        byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
        string dataUri = $"data:{attachment.ContentType};base64,{Convert.ToBase64String(fileBytes)}";
        return new DataContent(dataUri, attachment.ContentType);
    }

    public string? GetFilePath(string userId, string storedFileName)
    {
        string safeFileName = Path.GetFileName(storedFileName);
        string filePath = Path.Combine(GetUploadFolder(userId), safeFileName);
        if (!File.Exists(filePath))
        {
            return null;
        }

        return filePath;
    }

    private string GetRequiredFilePath(string userId, string storedFileName)
    {
        string? filePath = GetFilePath(userId, storedFileName);
        if (filePath is null)
        {
            throw new FileNotFoundException("Uploaded file was not found.", storedFileName);
        }

        return filePath;
    }

    private string GetUploadFolder(string userId)
    {
        return Path.Combine(webHostEnvironment.ContentRootPath, "App_Data", "file-uploads", GetUserFolderName(userId));
    }

    private static string GetUserFolderName(string userId)
    {
        byte[] userIdBytes = Encoding.UTF8.GetBytes(userId);
        byte[] hashBytes = SHA256.HashData(userIdBytes);
        return Convert.ToHexString(hashBytes);
    }
}
