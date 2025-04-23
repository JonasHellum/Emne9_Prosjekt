using Emne9_Prosjekt.Features.Leaderboards.Interfaces;
using Emne9_Prosjekt.Features.Leaderboards.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Emne9_Prosjekt.Features.Leaderboards;


[ApiController]
[Route("api/leaderboards")]
public class LeaderboardController : ControllerBase
{
    private readonly ILogger<LeaderboardController> _logger;
    private readonly ILeaderboardService _leaderboardService;
    private readonly IConfiguration _config;

    public LeaderboardController(ILogger<LeaderboardController> logger, 
        ILeaderboardService leaderboardService,
        IConfiguration config)
    {
        _logger = logger;
        _leaderboardService = leaderboardService;
        _config = config;
    }
    
    [Authorize]
    [HttpPut("updateOrCreate", Name = "UpdateOrCreateLeaderboard")]
    public async Task<IActionResult> UpdateOrCreateLeaderboard([FromBody] LeaderboardAddOrUpdateDTO leaderboardDto, 
        [FromHeader(Name = "X-Blazor-Secret")] string blazorSecretHeader)
    {
        var secret = _config["AppSettings:BlazorSecret"];
        
        _logger.LogDebug($"Config Secret: {secret}");
        _logger.LogDebug($"Header Secret: {blazorSecretHeader}");

        if (string.IsNullOrEmpty(blazorSecretHeader) || blazorSecretHeader != secret)
        {
            return Unauthorized("Unauthorized access.");
        }
        
        _logger.LogInformation("Updating or creating leaderboard");
        if (leaderboardDto == null)
        {
            return BadRequest("Leaderboard data is required.");
        }
        
        var leaderboard = await _leaderboardService.AddOrUpdateAsync(leaderboardDto);
        return leaderboard is null
            ? BadRequest("Failed to update or create new leaderboard")
            : Ok(leaderboard);
        
    }

}