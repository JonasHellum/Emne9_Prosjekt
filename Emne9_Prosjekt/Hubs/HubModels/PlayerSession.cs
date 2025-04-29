namespace Emne9_Prosjekt.Hubs.HubModels;

public class PlayerSession
{
    
    public string ConnectionId { get; set; } = "";
    public Dictionary<string, int> Board { get; set; } = new();
}