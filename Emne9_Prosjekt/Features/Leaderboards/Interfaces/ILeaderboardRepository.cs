using Emne9_Prosjekt.Features.Common.Interfaces;
using Emne9_Prosjekt.Features.Leaderboards.Models;

namespace Emne9_Prosjekt.Features.Leaderboards.Interfaces;

public interface ILeaderboardRepository : IBaseRepository<Leaderboard>
{
    Task<Leaderboard?> GetByIdAndGameType(Guid memberId, string gameType);
}