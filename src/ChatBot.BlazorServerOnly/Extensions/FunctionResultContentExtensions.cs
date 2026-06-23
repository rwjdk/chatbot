using Microsoft.Extensions.AI;

namespace ChatBot.BlazorServerOnly.Extensions;

public static class FunctionResultContentExtensions
{
    public static string AsDisplayString(this FunctionResultContent content)
    {
        return content.Result?.ToString() ?? "???";
    }
}