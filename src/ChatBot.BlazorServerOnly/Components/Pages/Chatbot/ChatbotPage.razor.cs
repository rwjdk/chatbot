using System.Text.Json;
using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.OpenAI;
using AgentFrameworkToolkit.Tools.Common;
using ChatBot.BlazorServerOnly.Models;
using JetBrains.Annotations;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.JSInterop;

namespace ChatBot.BlazorServerOnly.Components.Pages.Chatbot;

[UsedImplicitly]
public partial class ChatbotPage(
    AzureOpenAIAgentFactory azureOpenAIAgentFactory,
    StoredConversationsService storedConversationsService,
    ILocalStorageService localStorageService,
    OpenWeatherMapOptions openWeatherMapOptions)
{
    private const string StreamingLocalStorageKey = "chatbot.streaming";

    private string? _input;
    private bool _streaming;
    private string? _streamedResponse;
    private string? _streamedReasoning;
    private List<AIContent> _streamedContent = [];
    private AzureOpenAIAgent? _agent;
    private Conversation? _currentConversation;
    private Components.LeftSidebar? _leftSidebar;

    protected override async Task OnInitializedAsync()
    {
        _agent = azureOpenAIAgentFactory.CreateAgent(new AgentOptions
        {
            ClientType = ClientType.ResponsesApi,
            Model = OpenAIChatModels.Gpt5Mini,
            ReasoningEffort = OpenAIReasoningEffort.Medium,
            ReasoningSummaryVerbosity = OpenAIReasoningSummaryVerbosity.Detailed,
            Tools = [WeatherTools.GetWeatherForCity(openWeatherMapOptions)]
        });
        _currentConversation = Conversation.NewConversation();

        _streaming = await localStorageService.GetItemAsync<bool>(StreamingLocalStorageKey);
    }

    private void NewChat()
    {
        _currentConversation = Conversation.NewConversation();
        ResetMidStreamingValues();
    }

    private async Task SetStreamingAsync(bool streaming)
    {
        _streaming = streaming;
        await localStorageService.SetItemAsync(StreamingLocalStorageKey, streaming);
    }

    private async Task SendMessageAsync()
    {
        if (_agent == null || _currentConversation == null)
        {
            return;
        }

        string? input = _input?.Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        ResetMidStreamingValues();

        if (_currentConversation.MissingTitle)
        {
            AzureOpenAIAgent titleGenerationAgent = azureOpenAIAgentFactory.CreateAgent(OpenAIChatModels.Gpt41Nano);
            AgentResponse<string> response = await titleGenerationAgent.RunAsync<string>($"Given the following message: '{input}' generate a max 25 char long title for this question");
            _currentConversation.Title = response.Result;
            _leftSidebar?.AddConversation(_currentConversation);
        }

        _currentConversation.AddUserMessage(input);
        await InvokeAsync(StateHasChanged);

        if (_streaming)
        {
            List<AgentResponseUpdate> updates = [];
            await foreach (AgentResponseUpdate update in _agent.RunStreamingAsync(_currentConversation.GetRawMessages()))
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

            ResetMidStreamingValues();
            AgentResponse response = updates.ToAgentResponse();
            _currentConversation.AddDataFromAgentResponse(response);
        }
        else
        {
            AgentResponse response = await _agent.RunAsync(_currentConversation.GetRawMessages());
            _currentConversation.AddDataFromAgentResponse(response);
        }
        await InvokeAsync(StateHasChanged);

        await storedConversationsService.StoreSessionAsync(_currentConversation);
    }

    private void ResetMidStreamingValues()
    {
        _input = null;
        _streamedReasoning = null;
        _streamedResponse = null;
        _streamedContent = [];
    }

    private void SwitchSession(Conversation conversation)
    {
        _currentConversation = conversation;
        ResetMidStreamingValues();
    }

    private FunctionResultContent? FindFunctionResultContent(string callId)
    {
        return _currentConversation == null ? null : FindFunctionResultContent(callId, _currentConversation.Messages.SelectMany(x => x.Contents));
    }

    private static FunctionResultContent? FindFunctionResultContent(string callId, IEnumerable<AIContent> contents)
    {
        return contents.OfType<FunctionResultContent>().FirstOrDefault(x => x.CallId == callId);
    }

    private bool HasFunctionCallContent(string callId)
    {
        if (_currentConversation == null)
        {
            return false;
        }

        return HasFunctionCallContent(callId, _currentConversation.Messages.SelectMany(x => x.Contents));
    }

    private static bool HasFunctionCallContent(string callId, IEnumerable<AIContent> contents)
    {
        return contents.OfType<FunctionCallContent>().Any(x => x.CallId == callId);
    }
}
