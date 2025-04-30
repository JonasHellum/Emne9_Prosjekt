using Emne9_Prosjekt.Features.Common.Interfaces;
using Emne9_Prosjekt.Features.Members.Models;

namespace Emne9_Prosjekt.Features.Members.Interfaces;

public interface IMemberRepository : IBaseRepository<Member>
{
    Task<bool> UserNameExistsAsync(string username);
    Task<Member?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    Task<Member?> GetByIdAsync(Guid memberId);

}