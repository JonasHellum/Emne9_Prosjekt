using Emne9_Prosjekt.Features.Common.Interfaces;
using Emne9_Prosjekt.Features.Leaderboards.Models;

namespace Emne9_Prosjekt.Features.Leaderboards.Mappers;

public class LeaderboardMapper : IMapper<Leaderboard, LeaderboardDTO>
{
    public LeaderboardDTO MapToDTO(Leaderboard model)
    {
        return new LeaderboardDTO()
        {
            LeaderboardId = model.LeaderboardId,
            MemberId = model.MemberId,
            GameType = model.GameType,
            Wins = model.Wins,
            Losses = model.Losses,
            LastUpdated = model.LastUpdated
        };
    }

    public Leaderboard MapToModel(LeaderboardDTO dto)
    {
        return new Leaderboard()
        {
            LeaderboardId = dto.LeaderboardId,
            MemberId = dto.MemberId,
            GameType = dto.GameType,
            Wins = dto.Wins,
            Losses = dto.Losses,
            LastUpdated = dto.LastUpdated
        };
    }
}