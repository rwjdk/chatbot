using ChatBot.BlazorServerOnly.Models;
using ChatBot.BlazorServerOnly.Services;
using Microsoft.AspNetCore.Components;

namespace ChatBot.BlazorServerOnly.Components.Pages.Chatbot.Components;

public partial class RightSidebar(UserPersonalizationService userPersonalizationService)
{
    private string? _customInstructions;

    [Parameter,EditorRequired] public required string UserId { get; set; }

    [Parameter, EditorRequired] public bool Streaming { get; set; }

    [Parameter, EditorRequired] public EventCallback<bool> StreamingChanged { get; set; }

    [Parameter, EditorRequired] public ImageGenStyle SelectedImageGenStyle { get; set; }

    [Parameter, EditorRequired] public EventCallback<ImageGenStyle> SelectedImageGenStyleChanged { get; set; }

    protected override void OnInitialized()
    {
        _customInstructions = userPersonalizationService.GetPersonalization(UserId)?.CustomerInstructions;
    }

    private Task SetStreamingAsync(bool streaming)
    {
        return StreamingChanged.InvokeAsync(streaming);
    }

    private Task SetImageGenStyleAsync(ImageGenStyle imageGenStyle)
    {
        return SelectedImageGenStyleChanged.InvokeAsync(imageGenStyle);
    }

    private void SaveCustomInstructions()
    {
        UserPersonalization? personalization = userPersonalizationService.GetPersonalization(UserId);
        personalization ??= new UserPersonalization
        {
            Memories = [],
        };
        personalization.CustomerInstructions = _customInstructions;
        userPersonalizationService.SavePersonalization(UserId, personalization);
    }
}