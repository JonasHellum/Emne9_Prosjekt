using System.Security.Claims;
using Emne9_Prosjekt.Features.Common.Interfaces;
using Emne9_Prosjekt.Features.Members.Models;
using Google.Apis.Auth;

namespace Emne9_Prosjekt.Features.Members.Interfaces;

public interface IMemberService : IBaseService<MemberDTO>
{
    Task<MemberDTO?> RegistrationAsync(MemberRegistrationDTO registrationDTO);
    Task<MemberDTO?> LoginMemberAsync(string username, string password);
    Task<MemberDTO?> GoogleLoginAsync(GoogleJsonWebSignature.Payload googleUser);
    string ValidateAccessToken(string accessToken);
    
    string MakeToken(MemberDTO member);
    
    //(string? userId, IEnumerable<string>? roles) ValidateAccessToken(string accessToken);
    //Task<MemberDTO?> UpdateAsync(int id, MemberUpdateDTO updateDTO);
//     Task<IEnumerable<MemberDTO?>> GetPagedAsync(int pageNumber, int pageSize);
}