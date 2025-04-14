namespace Emne9_Prosjekt.Hubs.HubModels;

public class Comment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MessageId { get; set; }
    public string Text { get; set; } = string.Empty;
}