namespace Emne9_Prosjekt.Hubs.HubModels;

public class GameSession
{
    public string GameId { get; set; } = "";
    public PlayerSession Player1 { get; set; } = new();
    public PlayerSession Player2 { get; set; } = new();
    public string CurrentTurnConnectionId { get; set; } = "";
}