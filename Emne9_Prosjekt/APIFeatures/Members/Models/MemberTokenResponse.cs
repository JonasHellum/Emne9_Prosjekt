namespace Emne9_Prosjekt.Features.Members.Models;

public class MemberTokenResponse
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }

}

public class MemberTokenRequest
{
    public string RefreshToken { get; set; }
}
