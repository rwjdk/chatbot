using AgentFrameworkToolkit;
using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.OpenAI;
using JetBrains.Annotations;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ChatBot.BlazorServerOnly.Components.Pages;

[UsedImplicitly]
public partial class Home(AzureOpenAIAgentFactory azureOpenAIAgentFactory)
{
    private string? _input;
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
        
        AgentResponse response = await _agent.RunAsync(content, _session);
        _input = null;
    }
}
