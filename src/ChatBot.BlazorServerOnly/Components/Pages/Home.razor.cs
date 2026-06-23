using AgentFrameworkToolkit;
using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.OpenAI;
using AgentFrameworkToolkit.Tools.Common;
using ChatBot.BlazorServerOnly.Extensions;
using JetBrains.Annotations;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ChatBot.BlazorServerOnly.Components.Pages;

[UsedImplicitly]
public partial class Home(
    AzureOpenAIAgentFactory azureOpenAIAgentFactory,
    StoredSessionsService storedSessionsService,
    OpenWeatherMapOptions openWeatherMapOptions)
{
    private string? _input;
    private bool _streaming;
    private string? _streamedResponse;
    private string? _streamedReasoning;
    private List<AIContent> _streamedContent = [];
    private AzureOpenAIAgent? _agent;
    private AgentSession? _currentSession;
    private List<AgentSession> _previousSessions = [];
    private string? _currentPrompt;
    private UsageDetails? _usageDetails;

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
        _previousSessions = await storedSessionsService.LoadPreviousSessionsAsync(_agent);
        _currentSession = await _agent.CreateSessionAsync();
    }

    private async Task NewChatAsync()
    {
        if (_agent == null)
        {
            return;
        }

        _currentSession = await _agent.CreateSessionAsync();
        ResetTurnValues();
    }

    private async Task SendMessageAsync()
    {
        if (_agent == null || _currentSession == null)
        {
            return;
        }

        string? input = _input?.Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        ResetTurnValues();
        bool newSession = await _currentSession.GenerateTitleForSessionIfNeededAsync(
            titleGenerationAgent: azureOpenAIAgentFactory.CreateAgent(OpenAIChatModels.Gpt41Nano),
            input
        );
        if (newSession)
        {
            _previousSessions.Add(_currentSession);
        }

        _currentPrompt = input;
        await InvokeAsync(StateHasChanged);

        if (_streaming)
        {
            List<AgentResponseUpdate> updates = [];
            await foreach (AgentResponseUpdate update in _agent.RunStreamingAsync(input, _currentSession))
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

            ResetTurnValues();
            _usageDetails = updates.ToAgentResponse().Usage;
        }
        else
        {
            AgentResponse response = await _agent.RunAsync(input, _currentSession);
            _usageDetails = response.Usage;
        }

        _currentPrompt = null;
        await InvokeAsync(StateHasChanged);

        await storedSessionsService.StoreSessionAsync(_agent, _currentSession);
    }

    private void ResetTurnValues()
    {
        _input = null;
        _streamedReasoning = null;
        _streamedResponse = null;
        _streamedContent = [];
        _usageDetails = null;
    }

    private void SwitchSession(AgentSession session)
    {
        _currentSession = session;
        ResetTurnValues();
    }
}
