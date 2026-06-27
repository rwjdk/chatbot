using System.ClientModel;
using AgentFrameworkToolkit.AzureOpenAI;
using AgentFrameworkToolkit.OpenAI;
using AgentFrameworkToolkit.Tools.Common;
using Azure.AI.OpenAI;
using ChatBot.BlazorServerOnly.Models;
using ChatBot.BlazorServerOnly.Services;
using ChatBot.BlazorServerOnly.Tools;
using JetBrains.Annotations;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.JSInterop;
using OpenAI.Images;

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
    private ImageGenStyle _imageGenStyle;

    //Components
    private Components.LeftSidebar? _leftSidebar;
    private bool _inImageGenerationMode;

    protected override async Task OnInitializedAsync()
    {
        _streaming = await localStorageService.GetItemAsync<bool>(LocalStorageKeys.Streaming);
        _imageGenStyle = await localStorageService.GetItemAsync<ImageGenStyle>(LocalStorageKeys.ImageGenStyle);
    }

    private async Task SendAsync()
    {
        string? input = _input?.Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }
        ResetMidTurnValues();

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

        switch (_imageGenStyle)
        {
            case ImageGenStyle.RouterAgent:
                {
                    AzureOpenAIAgent routerAgent = azureOpenAIAgentFactory.CreateAgent(new AgentOptions
                    {
                        ClientType = ClientType.ChatClient,
                        Model = OpenAIChatModels.Gpt5Mini,
                        ReasoningEffort = OpenAIReasoningEffort.Low,
                        Instructions = "You are a router-agent determining what task the user is asking (being either generating an image (use can say show image, generate image, draw image, render image) or being a normal chatbot). If you are at all in doubt, go the chatbot route"
                    });

                    AgentResponse<TaskType> routerResponse = await routerAgent.RunAsync<TaskType>(_conversation.GetRawMessages());
                    switch (routerResponse.Result)
                    {
                        case TaskType.GenerateImageRoute:
                            await DoImageGenerationAsync();
                            break;
                        case TaskType.ChatBotRoute:
                            await AnswerWithChatbotAsync();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                break;
            case ImageGenStyle.ImageGenAsTool:
                await AnswerWithChatbotAsync();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        await InvokeAsync(StateHasChanged);
        await conversationsService.StoreConversationAsync(_conversation);
    }

    private async Task DoImageGenerationAsync()
    {
        _inImageGenerationMode = true;
        await InvokeAsync(StateHasChanged);
        string imageGenerationPrompt = _conversation.GetAsImageGenerationPrompt();
        await new ImageGenerationTool(azureOpenAIAgentFactory, _conversation).GenerateImageAsync(imageGenerationPrompt);
        ResetMidTurnValues();
    }

    private async Task AnswerWithChatbotAsync()
    {
        ImageGenerationTool imageGenerationTool = new(azureOpenAIAgentFactory, _conversation);
        AzureOpenAIAgent agent = azureOpenAIAgentFactory.CreateAgent(new AgentOptions
        {
            ClientType = ClientType.ResponsesApi,
            Model = OpenAIChatModels.Gpt5Mini,
            ReasoningEffort = OpenAIReasoningEffort.Medium,
            ReasoningSummaryVerbosity = OpenAIReasoningSummaryVerbosity.Detailed,
            Tools = [WeatherTools.GetWeatherForCity(openWeatherMapOptions), AIFunctionFactory.Create(imageGenerationTool.GenerateImageAsync, "generate_image")],
            Instructions = "You are a chatbot answering questions"
        });

        if (!_streaming)
        {
            await GenerateNonStreamingResponseAsync(agent);
        }
        else
        {
            await GenerateStreamingResponseAsync(agent);
        }
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
        List<ChatMessage> chatMessages = _conversation.GetRawMessages();
        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(chatMessages))
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

    private async Task SetImageGenStyleAsync(ImageGenStyle imageGenStyle)
    {
        _imageGenStyle = imageGenStyle;
        await localStorageService.SetItemAsync(LocalStorageKeys.ImageGenStyle, imageGenStyle);
    }

    private void ResetMidTurnValues()
    {
        _input = null;
        _streamedReasoning = null;
        _streamedResponse = null;
        _streamedContent = [];
        _inImageGenerationMode = false;
    }
}
