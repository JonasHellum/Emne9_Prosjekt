using Emne9_Prosjekt.Features.Common.Interfaces;
using Emne9_Prosjekt.Features.Members.Models;

namespace Emne9_Prosjekt.Features.Members.Interfaces;

public interface IMemberService : IBaseService<MemberDTO>
{
    Task<MemberDTO?> RegistrationAsync(MemberRegistrationDTO registrationDTO);
    Task<int?> AuthenticateMemberAsync(int firstName, string password);
    //Task<MemberDTO?> UpdateAsync(int id, MemberUpdateDTO updateDTO);
//     Task<IEnumerable<MemberDTO?>> GetPagedAsync(int pageNumber, int pageSize);
}