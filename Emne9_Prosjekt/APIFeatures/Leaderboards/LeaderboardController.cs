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

    /// <summary>
    /// Updates an existing leaderboard entry or creates a new one based on the provided data.
    /// </summary>
    /// <param name="leaderboardDto">The data transfer object containing leaderboard details such as game type, wins, and losses.</param>
    /// <param name="blazorSecretHeader">The secret authorization header for validating the request source.</param>
    /// <returns>An IActionResult indicating the result of the operation.
    /// Returns Ok with the updated or newly created leaderboard object on success.
    /// Returns Unauthorized if the provided secret is invalid.
    /// Returns BadRequest if the provided data is invalid or the operation fails.</returns>
    [Authorize]
    [HttpPut("updateOrCreate", Name = "UpdateOrCreateLeaderboard")]
    public async Task<IActionResult> UpdateOrCreateLeaderboard([FromBody] LeaderboardAddOrUpdateDTO leaderboardDto, 
        [FromHeader(Name = "X-Blazor-Secret")] string blazorSecretHeader)
    {
        _logger.LogInformation("Doing a put request to update or create a new leaderboard.");
        var secret = _config["AppSettings:BlazorSecret"];
        
        _logger.LogDebug($"Config Secret: {secret}");
        _logger.LogDebug($"Header Secret: {blazorSecretHeader}");

        if (string.IsNullOrEmpty(blazorSecretHeader) || blazorSecretHeader != secret)
        {
            _logger.LogWarning("Unauthorized access.");
            return Unauthorized("Unauthorized access.");
        }
        
        if (leaderboardDto == null)
        {
            _logger.LogWarning("Leaderboard data is required.");
            return BadRequest("Leaderboard data is required.");
        }
        
        var leaderboard = await _leaderboardService.AddOrUpdateAsync(leaderboardDto);
        return leaderboard is null
            ? BadRequest("Failed to update or create new leaderboard")
            : Ok(leaderboard);
    }

    /// <summary>
    /// Retrieves a paginated list of leaderboard entries for a specified game type.
    /// </summary>
    /// <param name="gameType">The type of game for which the leaderboard data is being requested.</param>
    /// <param name="page">The page number to retrieve. Defaults to 1.</param>
    /// <param name="pageSize">The number of entries to include per page. Defaults to 50.</param>
    /// <returns>An IActionResult containing the requested page of leaderboard entries.
    /// Returns Ok with the paginated leaderboard data on success.
    /// Returns BadRequest if the data retrieval operation fails.</returns>
    [AllowAnonymous]
    [HttpGet("{gameType}/paginated", Name = "PaginatedLeaderboard")]
    public async Task<IActionResult> GetPaginatedLeaderboard(string gameType, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        _logger.LogInformation("Doing a get request to fetch a paginated leaderboard.");
        var leaderboard = await _leaderboardService.GetLeaderboardPaginatedAsync(gameType, page, pageSize);

        return leaderboard is null
            ? BadRequest("Failed to fetch leaderboard data.")
            : Ok(leaderboard);
    }
}