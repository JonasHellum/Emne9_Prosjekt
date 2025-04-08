using Emne9_Prosjekt.Features.Members.Models;
using FluentValidation.Results;


namespace Emne9_Prosjekt.Validators.Interfaces;

public interface IAsyncMemberRegistrationValidator
{
    Task<ValidationResult> ValidateAsync(MemberRegistrationDTO registrationDTO, CancellationToken cancellationToken = default);

}