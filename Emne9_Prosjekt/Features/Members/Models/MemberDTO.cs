namespace Emne9_Prosjekt.Features.Members.Models;

public class MemberDTO
{
    public Guid MemberId { get; set; }
    
    public string GoogleId { get; set; } = string.Empty;
    
    public string UserName { get; set; } = string.Empty;
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public DateOnly BirthYear { get; set; }
    
    public DateOnly Created { get; set; }
    
    public DateOnly Updated { get; set; }
    
    public string Password { get; set; } = string.Empty;
}