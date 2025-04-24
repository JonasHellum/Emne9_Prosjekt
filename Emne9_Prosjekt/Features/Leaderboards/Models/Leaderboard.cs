using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Emne9_Prosjekt.Features.Leaderboards.Models;

public class Leaderboard
{
    [Key]
    public Guid LeaderboardId { get; set; }
    
    [ForeignKey("MemberId")]
    public Guid MemberId { get; set; }
    
    [ForeignKey("Username")]
    public string UserName { get; set; } = string.Empty;
    
    public string GameType { get; set; }  = string.Empty;
    
    public int Wins { get; set; }
    
    public int Losses { get; set; }
    
    public DateTime Created { get; set; }
    
    public DateTime LastUpdated { get; set; }
}