namespace Emne9_Prosjekt.Features.Members.Models;

public class MemberRegistrationDTO
{
    public string UserName { get; set; } = string.Empty;
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    public DateOnly BirthYear { get; set; } 
    
    public string Email { get; set; }
    
    public string Password { get; set; } = string.Empty;
}