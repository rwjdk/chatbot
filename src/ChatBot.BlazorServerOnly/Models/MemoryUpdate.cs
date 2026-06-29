using JetBrains.Annotations;

namespace ChatBot.BlazorServerOnly.Models;

[UsedImplicitly]
public record MemoryUpdate(List<string> MemoryToAdd, List<string> MemoryToRemove)
{
    public string GetDisplayString()
    {
        string memoryUpdateDisplayString = "Memory: ";
        if (MemoryToAdd.Count > 0)
        {
            memoryUpdateDisplayString += $" [Added {string.Join(", ", MemoryToAdd.Select(x => $"'{x}'"))}]";
        }
        if (MemoryToRemove.Count > 0)
        {
            memoryUpdateDisplayString += $" [Removed {string.Join(", ", MemoryToRemove.Select(x => $"'{x}'"))}]";
        }

        return memoryUpdateDisplayString;
    }
}