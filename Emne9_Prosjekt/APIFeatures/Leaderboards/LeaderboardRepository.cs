using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Emne9_Prosjekt.Data;
using Emne9_Prosjekt.Features.Leaderboards.Interfaces;
using Emne9_Prosjekt.Features.Leaderboards.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Emne9_Prosjekt.Features.Leaderboards;

public class LeaderboardRepository : ILeaderboardRepository
{
    private readonly Emne9EksamenDbContext _dbContext;
    private readonly ILogger<LeaderboardRepository> _logger;

    public LeaderboardRepository(Emne9EksamenDbContext dbContext, ILogger<LeaderboardRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }


    /// <summary>
    /// Adds a new leaderboard record to the database.
    /// </summary>
    /// <param name="entity">The leaderboard entity containing information to be added.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains
    /// the newly added leaderboard record if successful; otherwise, null.</returns>
    public async Task<Leaderboard?> AddAsync(Leaderboard entity)
    {
        _logger.LogDebug($"Adding new leaderboard with current values:" +
                         $"MemberId: {entity.MemberId}, UserName: {entity.UserName}, " +
                         $"Wins: {entity.Wins}, Losses: {entity.Losses}," +
                         $"Created: {entity.Created}, Updated: {entity.LastUpdated}");
        await _dbContext.Leaderboard.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation($"Added new leaderboard with id: {entity.LeaderboardId}");
        return entity;
    }

    public Task<Leaderboard?> DeleteByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Updates an existing leaderboard record with new values.
    /// </summary>
    /// <param name="entity">The leaderboard entity containing updated values.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains
    /// the updated leaderboard record if successful; otherwise, null.</returns>
    public async Task<Leaderboard?> UpdateAsync(Leaderboard entity)
    {
        _logger.LogDebug($"Finding leaderboard based on memberId: {entity.MemberId} and game type: {entity.GameType}");
        var leaderboard = await GetByIdAndGameType(entity.MemberId, entity.GameType);
        if (leaderboard == null) return null;
        
        _logger.LogDebug($"Updating leaderboard with id: {leaderboard.LeaderboardId} with current values: " +
                         $"from: {leaderboard.Wins} to: {entity.Wins} " +
                         $"from: {leaderboard.Losses} to: {entity.Losses} " +
                         $"from: {leaderboard.LastUpdated} to: {entity.LastUpdated}");
        
        _dbContext.Leaderboard.Update(leaderboard);
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation($"Updated leaderboard with id {leaderboard.LeaderboardId}");
        return leaderboard;
    }

    public Task<Leaderboard?> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Leaderboard>> FindAsync(Expression<Func<Leaderboard, bool>> predicate)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<Leaderboard>> GetPagedAsync(int pageNumber, int pageSize)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Retrieves a leaderboard record based on the provided member ID and game type.
    /// </summary>
    /// <param name="memberId">The unique identifier of the member.</param>
    /// <param name="gameType">The type of game associated with the leaderboard record.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains
    /// the leaderboard record if found; otherwise, null.</returns>
    public async Task<Leaderboard?> GetByIdAndGameType(Guid memberId, string gameType)
    {
        _logger.LogDebug($"Finding leaderboard with memberId: {memberId} and gameType: {gameType}");
        return await _dbContext.Leaderboard
            .FirstOrDefaultAsync(lb => lb.MemberId == memberId && lb.GameType.ToLower() == gameType.ToLower());
    }


    /// <summary>
    /// Retrieves a paginated list of leaderboard records + the logged-in member stats if logged in, ordered by wins and losses, and optionally filters by game type.
    /// </summary>
    /// <param name="gameType">The type of game to filter the leaderboard records, or "All" to include all game types.</param>
    /// <param name="page">The page number to retrieve (1-based index).</param>
    /// <param name="pageSize">The number of records to include in each page.</param>
    /// <param name="loggedInMemberId">An optional parameter representing the ID of the logged-in member for personalized rankings.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of paginated leaderboard records + the logged-in member stats.</returns>
    public async Task<List<LeaderboardDTO>> GetLeaderboardPaginatedAsync(string gameType, int page, int pageSize,
        Guid? loggedInMemberId = null)
    {
        // Step 1: Group and aggregate in the database
        var groupedQuery = _dbContext.Leaderboard
            .Where(l => gameType == "All" || l.GameType == gameType)
            .GroupBy(l => new { l.MemberId, l.UserName })
            .Select(group => new Leaderboard
            {
                MemberId = group.Key.MemberId,
                UserName = group.Key.UserName,
                Wins = group.Sum(g => g.Wins), 
                Losses = group.Sum(g => g.Losses) 
            })
            .OrderByDescending(l => l.Wins) 
            .ThenBy(l => l.Losses);         

        // Step 2: Load aggregated data into memory
        var aggregatedData = await groupedQuery.AsNoTracking().ToListAsync();

        // Step 3: Apply ranking in memory
        var rankedData = aggregatedData
            .Select((entry, index) => new LeaderboardDTO
            {
                MemberId = entry.MemberId,
                UserName = entry.UserName,
                Wins = entry.Wins,
                Losses = entry.Losses,
                Rank = index + 1
            })
            .ToList();

        
        var loggedInUser = rankedData.FirstOrDefault(r => r.MemberId == loggedInMemberId);
        if (loggedInUser == null && loggedInMemberId != null)
        {
            var userLeaderboard = await _dbContext.Leaderboard
                .Where(l => l.MemberId == loggedInMemberId && (gameType == "All" || l.GameType == gameType))
                .GroupBy(l => new { l.MemberId, l.UserName })
                .Select(group => new LeaderboardDTO
                {
                    MemberId = group.Key.MemberId,
                    UserName = group.Key.UserName,
                    Wins = group.Sum(g => g.Wins),
                    Losses = group.Sum(g => g.Losses)
                })
                .FirstOrDefaultAsync();
            if (userLeaderboard != null)
            {
                userLeaderboard.Rank = rankedData.Count + 1;
                rankedData.Add(userLeaderboard);
            }
        }

        // Step 5: Paginate the results
        var paginatedResult = rankedData
            .Skip((page - 1) * pageSize) 
            .Take(pageSize)
            .ToList();
        
        if (loggedInUser != null && !paginatedResult.Any(r => r.MemberId == loggedInMemberId))
        {
            // Manually add the logged-in user's stats to the result if not already present
            paginatedResult.Add(loggedInUser);
        }


        return paginatedResult;
    }
}