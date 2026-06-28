using ChatBot.BlazorServerOnly.Models;
using ChatBot.BlazorServerOnly.Services;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;

namespace ChatBot.BlazorServerOnly.Components.Pages.Chatbot.Components;

[UsedImplicitly]
public partial class LeftSidebar(ConversationsService conversationsService)
{
    private List<Conversation> _conversations = [];

    [Parameter]
    public EventCallback OnNewChat { get; set; }

    [Parameter]
    public EventCallback<Conversation> OnConversationSelected { get; set; }

    [Parameter]
    public string UserId { get; set; } = string.Empty;

    protected override async Task OnParametersSetAsync()
    {
        if (string.IsNullOrWhiteSpace(UserId))
        {
            _conversations = [];
            return;
        }

        _conversations = await conversationsService.LoadPreviousConversationsAsync(UserId);
    }

    public void AddConversation(Conversation conversation)
    {
        _conversations.Add(conversation);
        StateHasChanged();
    }
}
