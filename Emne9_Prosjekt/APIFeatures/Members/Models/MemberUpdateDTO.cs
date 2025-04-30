namespace Emne9_Prosjekt.Features.Members.Models;

public class MemberUpdateDTO
{
    public string UserName { get; set; } = string.Empty;
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    public DateOnly BirthYear { get; set; }
    
    public string Email { get; set; } = string.Empty;
    
    public string Password { get; set; } = string.Empty;
}