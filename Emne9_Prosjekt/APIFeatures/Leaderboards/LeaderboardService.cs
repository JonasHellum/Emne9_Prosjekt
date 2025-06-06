﻿using System.Data;
using Emne9_Prosjekt.Data;
using Emne9_Prosjekt.Features.Common.Interfaces;
using Emne9_Prosjekt.Features.Leaderboards.Interfaces;
using Emne9_Prosjekt.Features.Leaderboards.Models;
using Emne9_Prosjekt.Features.Members.Interfaces;
using Emne9_Prosjekt.Features.Members.Models;
using Microsoft.EntityFrameworkCore;

namespace Emne9_Prosjekt.Features.Leaderboards;

public class LeaderboardService : ILeaderboardService
{
    private readonly ILeaderboardRepository _leaderboardRepository;
    private readonly ILogger<LeaderboardService> _logger;
    private readonly IMapper<Leaderboard, LeaderboardDTO> _leaderboardMapper;
    private readonly IMapper<Leaderboard, LeaderboardAddOrUpdateDTO> _leaderboardAddMapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _config;
    private readonly Emne9EksamenDbContext _dbContext;
    private readonly IMemberRepository _memberRepository;

    public LeaderboardService(ILeaderboardRepository leaderboardRepository, 
        ILogger<LeaderboardService> logger, 
        IMapper<Leaderboard, LeaderboardDTO> leaderboardMapper, 
        IMapper<Leaderboard, LeaderboardAddOrUpdateDTO> leaderboardAddMapper, 
        IHttpContextAccessor httpContextAccessor, 
        IConfiguration config,
        Emne9EksamenDbContext dbContext,
        IMemberRepository memberRepository)
    {
        _leaderboardRepository = leaderboardRepository;
        _logger = logger;
        _leaderboardMapper = leaderboardMapper;
        _leaderboardAddMapper = leaderboardAddMapper;
        _httpContextAccessor = httpContextAccessor;
        _config = config;
        _dbContext = dbContext;
        _memberRepository = memberRepository;
    }


    public async Task<bool> DeleteByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<LeaderboardDTO?> GetByIdAsync(Guid memberId)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Adds a new leaderboard entry or updates an existing one based on the provided data.
    /// </summary>
    /// <param name="addOrUpdateDto">An instance of <see cref="LeaderboardAddOrUpdateDTO"/> containing the data for the leaderboard to add or update.</param>
    /// <returns>An instance of <see cref="LeaderboardDTO"/> representing the added or updated leaderboard, or null if the operation fails.</returns>
    /// <exception cref="DataException">Thrown when there is an error during the save or update operation in the repository.</exception>
    public async Task<LeaderboardDTO?> AddOrUpdateAsync(LeaderboardAddOrUpdateDTO addOrUpdateDto)
    {
        var loggedInMember = await GetLoggedInMemberAsync();

        if (loggedInMember == null)
        {
            _logger.LogWarning("No logged in member.");
            throw new UnauthorizedAccessException("No logged in member.");
        }
        
        _logger.LogDebug($"Trying to find leaderboard with memberId: {loggedInMember.MemberId} and gameType: {addOrUpdateDto.GameType}");
        var leaderboardToUpdate = await _leaderboardRepository.GetByIdAndGameType(loggedInMember.MemberId, addOrUpdateDto.GameType);

        if (leaderboardToUpdate != null)
        {
            _logger.LogDebug($"Trying to update leaderboard for member with memberId: {loggedInMember.MemberId}");
            leaderboardToUpdate.Wins += addOrUpdateDto.Wins;
            leaderboardToUpdate.Losses += addOrUpdateDto.Losses;
            leaderboardToUpdate.LastUpdated = DateTime.UtcNow;
            
            var updatedLeaderboard = await _leaderboardRepository.UpdateAsync(leaderboardToUpdate);

            if (updatedLeaderboard == null)
            {
                _logger.LogError($"Did not update leaderboard with LeaderboardId: {leaderboardToUpdate.LeaderboardId}.");
                throw new DataException($"Did not update leaderboard with LeaderboardId: {leaderboardToUpdate.LeaderboardId}.");
            }
            
            return _leaderboardMapper.MapToDTO(leaderboardToUpdate);
        }
        
        _logger.LogDebug($"Trying to add a new leaderboard for member with memberId: {loggedInMember.MemberId}");
        var leaderboard = _leaderboardAddMapper.MapToModel(addOrUpdateDto);
        leaderboard.MemberId = loggedInMember.MemberId;
        leaderboard.UserName = loggedInMember.UserName;
        leaderboard.Created = DateTime.UtcNow;
        leaderboard.LastUpdated = DateTime.UtcNow;
            
        var newLeaderboard = await _leaderboardRepository.AddAsync(leaderboard);
        if (newLeaderboard is null)
        {
            _logger.LogError("Failed to add leaderboard.");
            throw new DataException("Failed to add leaderboard.");
        }
            
        return _leaderboardMapper.MapToDTO(newLeaderboard);
    }

    /// <summary>
    /// Retrieves a paginated list of leaderboard records + the logged-in member stats if logged in, ordered by wins and losses, and optionally filters by game type.
    /// </summary>
    /// <param name="gameType">The type of game for which to retrieve the leaderboard entries.</param>
    /// <param name="page">The page number of the leaderboard entries to retrieve.</param>
    /// <param name="pageSize">The number of leaderboard entries to include per page.</param>
    /// <returns>A list of <see cref="LeaderboardDTO"/> representing the leaderboard entries for the specified game type and page.</returns>
    public async Task<List<LeaderboardDTO>> GetLeaderboardPaginatedAsync(string gameType, int page, int pageSize)
    {
        _logger.LogDebug($"Trying to get leaderboard for gameType: {gameType} and page: {page} and pageSize: {pageSize}");
        var loggedInMember = await GetLoggedInMemberAsync();
        
        var loggedInMemberId = loggedInMember?.MemberId;
        return await _leaderboardRepository.GetLeaderboardPaginatedAsync(gameType, page, pageSize, loggedInMemberId);
    }


    /// <summary>
    /// Retrieves the member who is currently logged in based on the context information.
    /// </summary>
    /// <returns>An instance of <see cref="Member"/> representing the logged-in member, or null if no member is logged in or the member cannot be found.</returns>
    private async Task<Member?> GetLoggedInMemberAsync()
    {
        var loggedInMemberId = _httpContextAccessor.HttpContext?.Items["MemberId"] as string;
        _logger.LogDebug("Logged in member ID: {LoggedInMemberId}", loggedInMemberId);
    
        if (string.IsNullOrEmpty(loggedInMemberId))
        {
            _logger.LogWarning("No logged in member.");
            return null;
        }
        
        var loggedInMember = (await _memberRepository.FindAsync(m => m.MemberId.ToString() == loggedInMemberId)).FirstOrDefault();
        if (loggedInMember == null)
        {
            _logger.LogWarning("Logged in member not found: {LoggedInMemberId}", loggedInMemberId);
            return null;
        }
        
        return loggedInMember;
    }
}