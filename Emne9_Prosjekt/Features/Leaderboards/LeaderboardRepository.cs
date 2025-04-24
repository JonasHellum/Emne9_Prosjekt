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
    /// Retrieves a list of leaderboard statistics, including total wins, losses, and last updated timestamp
    /// for each member grouped by their ID and username.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.
    /// The task result contains a list of leaderboard statistics.</returns>
    public async Task<List<Leaderboard>> GetAllLeaderboardStatsAsync()
    {
        _logger.LogDebug("Finding leaderboards.");
        // return await _dbContext.Leaderboard
        //     .GroupBy(l => new { l.MemberId, l.GameType })
        //     .Select(group => new Leaderboard
        //     {
        //         MemberId = group.Key.MemberId,
        //         UserName = group.FirstOrDefault().UserName,
        //         GameType = group.Key.GameType,
        //         Wins = group.Sum(l => l.Wins),
        //         Losses = group.Sum(l => l.Losses),
        //         LastUpdated = group.Max(l => l.LastUpdated)
        //     })
        //     .ToListAsync();
        
        return await _dbContext.Leaderboard
            .ToListAsync();
    }
    
    
    
    
    public async Task<List<LeaderboardDTO>> GetLeaderboardPaginatedAsync(int page, int pageSize, Guid loggedInMemberId)
    {
        // Step 1: Group and aggregate in the database
        var groupedQuery = _dbContext.Leaderboard
            .GroupBy(l => new { l.MemberId, l.UserName }) // Group by MemberId and Username
            .Select(group => new Leaderboard
            {
                MemberId = group.Key.MemberId,
                UserName = group.Key.UserName,
                Wins = group.Sum(g => g.Wins), // Total Wins
                Losses = group.Sum(g => g.Losses) // Total Losses
            })
            .OrderByDescending(l => l.Wins) // Primary sort: Wins descending
            .ThenBy(l => l.Losses);         // Secondary sort: Losses ascending

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
                Rank = index + 1 // Calculate rank (1-based index)
            })
            .ToList();

        
        var loggedInUser = rankedData.FirstOrDefault(r => r.MemberId == loggedInMemberId);
        if (loggedInUser == null)
        {
            var userLeaderboard = await _dbContext.Leaderboard
                .Where(l => l.MemberId == loggedInMemberId)
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
                userLeaderboard.Rank = rankedData.Count + 1; // Assign a rank beyond current list
                rankedData.Add(userLeaderboard);
            }
        }

        // Step 5: Paginate the results
        var paginatedResult = rankedData
            .Skip((page - 1) * pageSize) // Skip previous pages
            .Take(pageSize) // Take the items for the current page
            .ToList();
        
        if (loggedInUser != null && !paginatedResult.Any(r => r.MemberId == loggedInMemberId))
        {
            // Manually add the logged-in user's stats to the result if not already present
            paginatedResult.Add(loggedInUser);
        }


        return paginatedResult;
    }



    public async Task<List<LeaderboardDTO>> GetLeaderboardByGameTypePaginatedAsync(string gameType, int page, int pageSize, Guid loggedInMemberId)
    {
        // Step 1: Query filtered by GameType
        var groupedQuery = _dbContext.Leaderboard
            .Where(l => l.GameType == gameType) // Filter by GameType
            .GroupBy(l => new { l.MemberId, l.UserName }) // Group by MemberId and Username
            .Select(group => new Leaderboard
            {
                MemberId = group.Key.MemberId,
                UserName = group.Key.UserName,
                Wins = group.Sum(g => g.Wins),
                Losses = group.Sum(g => g.Losses)
            })
            .OrderByDescending(l => l.Wins) // Sort by Wins
            .ThenBy(l => l.Losses);         // Then by Losses

        // Step 2: Load to memory and apply ranking
        var aggregatedData = await groupedQuery.AsNoTracking().ToListAsync();
        var rankedData = aggregatedData
            .Select((entry, index) => new LeaderboardDTO
            {
                MemberId = entry.MemberId,
                UserName = entry.UserName,
                Wins = entry.Wins,
                Losses = entry.Losses,
                Rank = index + 1 // 1-based Rank
            })
            .ToList();

        // Step 3: Handle logged-in user inclusion (if not already in results)
        if (loggedInMemberId.ToString().IsNullOrEmpty())
        {
            var loggedInUser = rankedData.FirstOrDefault(r => r.MemberId == loggedInMemberId);
            if (loggedInUser == null)
            {
                var userLeaderboard = await _dbContext.Leaderboard
                    .Where(l => l.MemberId == loggedInMemberId && l.GameType == gameType)
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
        }

        // Step 4: Paginate
        return rankedData
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }



}