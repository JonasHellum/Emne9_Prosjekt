using Emne9_Prosjekt.Features.Common.Interfaces;
using Emne9_Prosjekt.Features.Leaderboards.Models;

namespace Emne9_Prosjekt.Features.Leaderboards.Interfaces;

public interface ILeaderboardService : IBaseService<LeaderboardDTO>
{
    Task<LeaderboardDTO?> AddOrUpdateAsync(LeaderboardAddOrUpdateDTO addOrUpdateDto);
}