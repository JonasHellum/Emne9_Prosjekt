namespace Emne9_Prosjekt.Features.Leaderboards.Models;

public class LeaderboardAddOrUpdateDTO
{
    public string GameType { get; set; }  = string.Empty;
    
    public int Wins { get; set; }
    
    public int Losses { get; set; }
}