using Emne9_Prosjekt.GameComponents;

namespace Emne9_Prosjekt.Hubs.HubModels;

public class Player
{
    public string ConnectionId { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public BattleShipComponents PlayerComponents { get; set; } = new();
    public bool IsReady { get; set; } 
    public bool IsTurn { get; set; }
}