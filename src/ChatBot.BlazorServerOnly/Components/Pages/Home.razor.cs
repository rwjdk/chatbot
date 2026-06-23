using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.OpenAI;
using AgentFrameworkToolkit.Tools.Common;
using ChatBot.BlazorServerOnly.Models;
using JetBrains.Annotations;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ChatBot.BlazorServerOnly.Components.Pages;

[UsedImplicitly]
public partial class Home(
    AzureOpenAIAgentFactory azureOpenAIAgentFactory,
    StoredConversationsService storedConversationsService,
    OpenWeatherMapOptions openWeatherMapOptions)
{
    private string? _input;
    private bool _streaming;
    private string? _streamedResponse;
    private string? _streamedReasoning;
    private List<AIContent> _streamedContent = [];
    private AzureOpenAIAgent? _agent;
    private Conversation? _currentConversation;
    private List<Conversation> _previousConversations = [];

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
        _previousConversations = await storedConversationsService.LoadPreviousConversationsAsync();
        _currentConversation = Conversation.NewConversation();
    }

    private void NewChat()
    {
        _currentConversation = Conversation.NewConversation();
        ResetMidStreamingValues();
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
            _previousConversations.Add(_currentConversation);
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
}
