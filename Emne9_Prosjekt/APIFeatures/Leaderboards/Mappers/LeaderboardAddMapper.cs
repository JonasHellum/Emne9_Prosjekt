using Emne9_Prosjekt.Features.Common.Interfaces;
using Emne9_Prosjekt.Features.Leaderboards.Models;

namespace Emne9_Prosjekt.Features.Leaderboards.Mappers;

public class LeaderboardAddMapper : IMapper<Leaderboard, LeaderboardAddOrUpdateDTO>
{
    public LeaderboardAddOrUpdateDTO MapToDTO(Leaderboard model)
    {
        return new LeaderboardAddOrUpdateDTO()
        {
            GameType = model.GameType,
            Wins = model.Wins,
            Losses = model.Losses
        };
    }

    public Leaderboard MapToModel(LeaderboardAddOrUpdateDTO dto)
    {
        return new Leaderboard()
        {
            GameType = dto.GameType,
            Wins = dto.Wins,
            Losses = dto.Losses,
        };
    }
}