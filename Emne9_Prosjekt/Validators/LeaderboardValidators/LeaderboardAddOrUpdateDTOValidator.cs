using Emne9_Prosjekt.Features.Leaderboards.Models;
using Emne9_Prosjekt.Features.Members.Models;
using FluentValidation;

namespace Emne9_Prosjekt.Validators.LeaderboardValidators;

public class LeaderboardAddOrUpdateDTOValidator : AbstractValidator<LeaderboardAddOrUpdateDTO>
{
    public LeaderboardAddOrUpdateDTOValidator()
    {
        RuleFor(x => x.GameType).NotEmpty().WithMessage("GameType is required")
            .Must(gameType => gameType == "Battleships" || gameType == "Connect 4")
            .WithMessage("GameType must be 'Battleships' or 'Connect 4'");
        
        RuleFor(x => x.Losses).NotEmpty().WithMessage("Losses value is required")
            .LessThanOrEqualTo(1).GreaterThanOrEqualTo(-1).WithMessage("Losses value must be between -1 and 1");
        
        RuleFor(x => x.Wins).LessThanOrEqualTo(1).GreaterThanOrEqualTo(-1)
            .WithMessage("Wins value must be between -1 and 1");
    }
}