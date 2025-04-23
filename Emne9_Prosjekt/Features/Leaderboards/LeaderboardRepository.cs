using System.Linq.Expressions;
using Emne9_Prosjekt.Data;
using Emne9_Prosjekt.Features.Leaderboards.Interfaces;
using Emne9_Prosjekt.Features.Leaderboards.Models;
using Microsoft.EntityFrameworkCore;

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
                         $"MemberId: {entity.MemberId}, Wins: {entity.Wins}, " +
                         $"Losses: {entity.Losses}, Created: {entity.Created}, " +
                         $"Updated: {entity.LastUpdated}");
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

    public async Task<Leaderboard?> GetByIdAndGameType(Guid memberId, string gameType)
    {
        _logger.LogDebug($"Finding leaderboard with memberId: {memberId} and gameType: {gameType}");
        return await _dbContext.Leaderboard
            .FirstOrDefaultAsync(lb => lb.MemberId == memberId && lb.GameType == gameType);
    }
}