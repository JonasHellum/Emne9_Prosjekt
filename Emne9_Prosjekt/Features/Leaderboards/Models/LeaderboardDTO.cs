namespace Emne9_Prosjekt.Features.Leaderboards.Models;

public class LeaderboardDTO
{
    public Guid LeaderboardId { get; set; }
    
    public Guid MemberId { get; set; }
    
    public string GameType { get; set; }  = string.Empty;
    
    public int Wins { get; set; }
    
    public int Losses { get; set; }
    
    public DateTime LastUpdated { get; set; }
}