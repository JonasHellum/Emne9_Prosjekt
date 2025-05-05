using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Emne9_Prosjekt.Features.Members.Models;

public class MemberRefreshToken
{
    [Key]
    public Guid TokenId { get; set; }
    
    public string Token { get; set; } = string.Empty;
    
    [ForeignKey("MemberId")]
    public Guid MemberId { get; set; }
    
    public string IpAddress { get; set; } = string.Empty;
    
    public DateTime Created { get; set; }
    
    public DateTime Expires { get; set; }
    
    public bool Revoked { get; set; }

}