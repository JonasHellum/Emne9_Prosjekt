﻿using System.Security.Claims;
using Emne9_Prosjekt.Features.Common.Interfaces;
using Emne9_Prosjekt.Features.Members.Models;
using Google.Apis.Auth;

namespace Emne9_Prosjekt.Features.Members.Interfaces;

public interface IMemberService : IBaseService<MemberDTO>
{
    Task<MemberDTO?> RegistrationAsync(MemberRegistrationDTO registrationDTO);
    Task<MemberDTO?> LoginMemberAsync(string username, string password);
    Task<MemberDTO?> GoogleLoginAsync(GoogleJsonWebSignature.Payload googleUser);
    (string memberId, string userName) ValidateAccessToken(string accessToken);
    Task<MemberDTO?> UpdateAsync(Guid memberId, MemberUpdateDTO updateDTO);
    string MakeAccessToken(MemberDTO member);
    string MakeRefreshToken();
    Task<bool> UserNameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
    Task SaveRefreshTokenAsync(Guid memberId, string refreshToken, string ipAdress);
    Task<Guid> ValidateRefreshTokenAsync(MemberTokenRequest token);
    Task RevokeRefreshTokenAsync(string token);
    Task<MemberRefreshToken> GetStoredRefreshTokenAsync(string token);

    //(string? userId, IEnumerable<string>? roles) ValidateAccessToken(string accessToken);
    //
//     Task<IEnumerable<MemberDTO?>> GetPagedAsync(int pageNumber, int pageSize);
}