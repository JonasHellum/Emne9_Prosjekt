using Emne9_Prosjekt.Features.Members.Models;
using FluentValidation.Results;

namespace Emne9_Prosjekt.Validators.Interfaces;

public interface IAsyncMemberUpdateValidator
{
    Task<ValidationResult> ValidateAsync(MemberUpdateDTO registrationDTO, CancellationToken cancellationToken = default);
}