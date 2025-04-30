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
            UserName = model.UserName,
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
            UserName = dto.UserName,
            GameType = dto.GameType,
            Wins = dto.Wins,
            Losses = dto.Losses,
            LastUpdated = dto.LastUpdated
        };
    }
}