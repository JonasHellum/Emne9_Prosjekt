using System.Data;
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
    
    public async Task<LeaderboardDTO?> AddOrUpdateAsync(LeaderboardAddOrUpdateDTO addOrUpdateDto)
    {
        var loggedInMember = await GetLoggedInMemberAsync();
        
        _logger.LogDebug($"Trying to find leaderboard with memberId: {loggedInMember.MemberId} and gameType: {addOrUpdateDto.GameType}");
        var leaderboardToUpdate = await _leaderboardRepository.GetByIdAndGameType(loggedInMember.MemberId, addOrUpdateDto.GameType);

        if (leaderboardToUpdate != null)
        {
            _logger.LogInformation($"Trying to update leaderboard for member with memberId: {loggedInMember.MemberId}");
            // Update existing record
            leaderboardToUpdate.Wins += addOrUpdateDto.Wins; // Add new wins to the existing wins
            leaderboardToUpdate.Losses += addOrUpdateDto.Losses; // Add new losses to the existing losses
            leaderboardToUpdate.LastUpdated = DateTime.UtcNow;
            
            var updatedLeaderboard = await _leaderboardRepository.UpdateAsync(leaderboardToUpdate);

            if (updatedLeaderboard == null)
            {
                _logger.LogError($"Did not update leaderboard with LeaderboardId: {leaderboardToUpdate.LeaderboardId}.");
                throw new DataException($"Did not update leaderboard with LeaderboardId: {leaderboardToUpdate.LeaderboardId}.");
            }
            
            
            return _leaderboardMapper.MapToDTO(leaderboardToUpdate);
        }
        
        _logger.LogInformation($"Trying to add a new leaderboard for member with memberId: {loggedInMember.MemberId}");
        var leaderboard = _leaderboardAddMapper.MapToModel(addOrUpdateDto);
        leaderboard.MemberId = loggedInMember.MemberId;
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
    /// Retrieves the currently logged-in member based on the information stored in the HTTP context.
    /// </summary>
    /// <returns>An instance of <see cref="Member"/> representing the logged-in member, or throws an exception if no member is logged in.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when no member is logged in or the member cannot be found.</exception>
    private async Task<Member?> GetLoggedInMemberAsync()
    {
        var loggedInMemberId = _httpContextAccessor.HttpContext?.Items["MemberId"] as string;
        _logger.LogInformation("Logged in member ID: {LoggedInMemberId}", loggedInMemberId);
    
        if (string.IsNullOrEmpty(loggedInMemberId))
        {
            _logger.LogWarning("No logged in member.");
            throw new UnauthorizedAccessException("No logged in member.");
        }
        
        var loggedInMember = (await _memberRepository.FindAsync(m => m.MemberId.ToString() == loggedInMemberId)).FirstOrDefault();
        if (loggedInMember == null)
        {
            _logger.LogWarning("Logged in member not found: {LoggedInMemberId}", loggedInMemberId);
            throw new UnauthorizedAccessException("Logged in member ID not found.");
        }
        
        return loggedInMember;
    }
}