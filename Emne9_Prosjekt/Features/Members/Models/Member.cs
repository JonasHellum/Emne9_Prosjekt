using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Emne9_Prosjekt.Features.Members.Models;

public class Member
{
    [Key]
    public Guid Id { get; set; }
    
    public string UserName { get; set; } = string.Empty;
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    public DateOnly BirthYear { get; set; }
    
    public DateOnly Created { get; set; }
    
    public DateOnly Updated { get; set; }
    
    public string HashedPassword { get; set; } = string.Empty;
}