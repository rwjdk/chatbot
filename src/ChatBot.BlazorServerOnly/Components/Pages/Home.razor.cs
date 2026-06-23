using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.OpenAI;
using JetBrains.Annotations;
using Microsoft.Agents.AI;

namespace ChatBot.BlazorServerOnly.Components.Pages;

[UsedImplicitly]
public partial class Home(AzureOpenAIAgentFactory azureOpenAIAgentFactory)
{
    private string? _input;
    private bool _streaming;
    private string? _streamedResponse;
    private readonly List<string> _conversations = ["Current chat"];
    private AzureOpenAIAgent? _agent;
    private AgentSession? _session;

    protected override async Task OnInitializedAsync()
    {
        _agent = azureOpenAIAgentFactory.CreateAgent(new AgentOptions
        {
            Model = OpenAIChatModels.Gpt5Mini,
            ReasoningEffort = OpenAIReasoningEffort.Low,
            ChatHistoryProvider = new InMemoryChatHistoryProvider(),
        });
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
        _conversations.Insert(0, $"New chat {_conversations.Count + 1}");
    }

    private async Task SendMessageAsync()
    {
        if (_agent == null)
        {
            return;
        }

        string? content = _input?.Trim();

        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        _input = null;
        _streamedResponse = null;

        if (_streaming)
        {
            await foreach (AgentResponseUpdate update in _agent.RunStreamingAsync(content, _session))
            {
                _streamedResponse += update.Text;
                await InvokeAsync(StateHasChanged);
            }

            _streamedResponse = null;
        }
        else
        {
            await _agent.RunAsync(content, _session);
        }

        await InvokeAsync(StateHasChanged);
    }
}
