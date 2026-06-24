using ChatBot.BlazorServerOnly.Models;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;

namespace ChatBot.BlazorServerOnly.Components.Pages.Chatbot.Components;

[UsedImplicitly]
public partial class LeftSidebar(StoredConversationsService storedConversationsService)
{
    private List<Conversation> _conversations = [];

    [Parameter]
    public EventCallback OnNewChat { get; set; }

    [Parameter]
    public EventCallback<Conversation> OnConversationSelected { get; set; }

    protected override async Task OnInitializedAsync()
    {
        _conversations = await storedConversationsService.LoadPreviousConversationsAsync();
    }

    public void AddConversation(Conversation conversation)
    {
        _conversations.Add(conversation);
        StateHasChanged();
    }
}
