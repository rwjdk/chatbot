using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.OpenAI;
using ChatBot.BlazorServerOnly.Extensions;
using JetBrains.Annotations;
using Microsoft.Agents.AI;

namespace ChatBot.BlazorServerOnly.Components.Pages;

[UsedImplicitly]
public partial class Home(AzureOpenAIAgentFactory azureOpenAIAgentFactory, ChatMessageConversationService chatMessageConversationService)
{
    private string? _input;
    private bool _streaming;
    private string? _streamedResponse;
    private AzureOpenAIAgent? _agent;
    private AgentSession? _session;
    private List<AgentSession> _conversations = [];

    protected override async Task OnInitializedAsync()
    {
        _agent = azureOpenAIAgentFactory.CreateAgent(new AgentOptions
        {
            Model = OpenAIChatModels.Gpt5Mini,
            ReasoningEffort = OpenAIReasoningEffort.Low,
            ChatHistoryProvider = new InMemoryChatHistoryProvider(),
        });
        _conversations = await chatMessageConversationService.LoadConversationsAsync(_agent);

        _session = await _agent.CreateSessionAsync();
    }

    private async Task NewChatAsync()
    {
        if (_agent == null)
        {
            return;
        }

        _session = await _agent.CreateSessionAsync();
        _input = null;
        _streamedResponse = null;
    }

    private async Task SendMessageAsync()
    {
        if (_agent == null || _session == null)
        {
            return;
        }

        string? input = _input?.Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        _input = null;
        _streamedResponse = null;

        bool newSession = await _session.GenerateTitleForSessionIfNeededAsync(
            titleGenerationAgent: azureOpenAIAgentFactory.CreateAgent(OpenAIChatModels.Gpt41Nano),
            input
        );
        if (newSession)
        {
            _conversations.Add(_session);
        }

        if (_streaming)
        {
            await foreach (AgentResponseUpdate update in _agent.RunStreamingAsync(input, _session))
            {
                _streamedResponse += update.Text;
                await InvokeAsync(StateHasChanged);
            }

            _streamedResponse = null;
        }
        else
        {
            await _agent.RunAsync(input, _session);
        }

        await InvokeAsync(StateHasChanged);

        await chatMessageConversationService.StoreConversationAsync(_agent, _session);
    }
}
