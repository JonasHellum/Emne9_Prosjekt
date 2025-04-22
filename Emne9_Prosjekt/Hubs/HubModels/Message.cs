namespace Emne9_Prosjekt.Hubs.HubModels;

public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<Comment> Comments { get; set; } = new();
}