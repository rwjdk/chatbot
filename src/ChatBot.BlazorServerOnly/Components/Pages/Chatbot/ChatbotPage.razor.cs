using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.OpenAI;
using AgentFrameworkToolkit.Tools.Common;
using ChatBot.BlazorServerOnly.Models;
using ChatBot.BlazorServerOnly.Services;
using JetBrains.Annotations;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.JSInterop;

namespace ChatBot.BlazorServerOnly.Components.Pages.Chatbot;

[UsedImplicitly]
public partial class ChatbotPage(
    AzureOpenAIAgentFactory azureOpenAIAgentFactory,
    ConversationsService conversationsService,
    ILocalStorageService localStorageService,
    OpenWeatherMapOptions openWeatherMapOptions)
{
    //Input and Conversation
    private string? _input;
    private Conversation _conversation = Conversation.NewConversation();

    //Streaming values
    private bool _streaming;
    private string? _streamedResponse;
    private string? _streamedReasoning;
    private List<AIContent> _streamedContent = [];
    
    //Components
    private Components.LeftSidebar? _leftSidebar;

    protected override async Task OnInitializedAsync()
    {
        _streaming = await localStorageService.GetItemAsync<bool>(LocalStorageKeys.Streaming);
    }

    private async Task SendAsync()
    {
        string? input = _input?.Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }
        ResetMidTurnValues();

        AzureOpenAIAgent agent = azureOpenAIAgentFactory.CreateAgent(new AgentOptions
        {
            ClientType = ClientType.ResponsesApi,
            Model = OpenAIChatModels.Gpt5Mini,
            ReasoningEffort = OpenAIReasoningEffort.Medium,
            ReasoningSummaryVerbosity = OpenAIReasoningSummaryVerbosity.Detailed,
            Tools = [WeatherTools.GetWeatherForCity(openWeatherMapOptions)],
        });
        

        if (_conversation.MissingATitle)
        {
            AzureOpenAIAgent titleGenerationAgent = azureOpenAIAgentFactory.CreateAgent(OpenAIChatModels.Gpt41Nano);
            string message = $"Given the following message: '{input}' generate a max 25 char long title for this question";
            AgentResponse<string> response = await titleGenerationAgent.RunAsync<string>(message);
            _conversation.Title = response.Result;
            _leftSidebar?.AddConversation(_conversation);
        }

        _conversation.AddUserMessage(input);
        await InvokeAsync(StateHasChanged);

        if (!_streaming)
        {
            await GenerateNonStreamingResponseAsync(agent);
        }
        else
        {
            await GenerateStreamingResponseAsync(agent);
        }

        await InvokeAsync(StateHasChanged);
        await conversationsService.StoreConversationAsync(_conversation);
    }

    private async Task GenerateNonStreamingResponseAsync(AzureOpenAIAgent agent)
    {
        List<ChatMessage> chatMessages = _conversation.GetRawMessages();
        AgentResponse response = await agent.RunAsync(chatMessages);
        _conversation.AddDataFromAgentResponse(response);
    }

    private async Task GenerateStreamingResponseAsync(AzureOpenAIAgent agent)
    {
        List<AgentResponseUpdate> updates = [];
        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(_conversation.GetRawMessages()))
        {
            updates.Add(update);
            foreach (AIContent content in update.Contents)
            {
                switch (content)
                {
                    case TextReasoningContent textReasoningContent:
                        _streamedReasoning += textReasoningContent.Text;
                        break;
                    default:
                        _streamedContent.Add(content);
                        break;
                }
            }

            _streamedResponse += update.Text;
            await InvokeAsync(StateHasChanged);
        }

        ResetMidTurnValues();
        AgentResponse response = updates.ToAgentResponse();
        _conversation.AddDataFromAgentResponse(response);
    }

    private void NewChat()
    {
        _conversation = Conversation.NewConversation();
        ResetMidTurnValues();
    }

    private void SwitchSession(Conversation conversation)
    {
        _conversation = conversation;
        ResetMidTurnValues();
    }

    private async Task SetStreamingAsync(bool streaming)
    {
        _streaming = streaming;
        await localStorageService.SetItemAsync(LocalStorageKeys.Streaming, streaming);
    }

    private void ResetMidTurnValues()
    {
        _input = null;
        _streamedReasoning = null;
        _streamedResponse = null;
        _streamedContent = [];
    }
}
