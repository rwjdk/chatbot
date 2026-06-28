using ChatBot.BlazorServerOnly.Components;
using ChatBot.BlazorServerOnly.Services;
using Microsoft.AspNetCore.StaticFiles;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults(); //From Aspire Service Defaults
builder.Services.AddSingleton<ConversationsService>();
builder.Services.AddSingleton<FileUploadStorageService>();
builder.Services.AddSingleton<ConversationChatMessageMapper>();
builder.Services.AddLocalStorageServices();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAntiforgery();

app.MapGet("/attachments/{storedFileName}", (string storedFileName, FileUploadStorageService fileUploadStorageService) =>
{
    string? filePath = fileUploadStorageService.GetFilePath(storedFileName);
    if (filePath is null)
    {
        return Results.NotFound();
    }

    FileExtensionContentTypeProvider contentTypeProvider = new();
    if (!contentTypeProvider.TryGetContentType(filePath, out string? contentType))
    {
        contentType = "application/octet-stream";
    }

    return Results.File(filePath, contentType);
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
