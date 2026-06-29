namespace ChatBot.BlazorServerOnly.Models;

public class UserPersonalization
{
    public string? CustomerInstructions { get; set; }
    public required List<string> Memories { get; set; }
}