using System.Data;
using System.Security.Claims;
using Emne9_Prosjekt.Features.Common.Interfaces;
using Emne9_Prosjekt.Features.Members.Interfaces;
using Emne9_Prosjekt.Features.Members.Models;

namespace Emne9_Prosjekt.Features.Members;

public class MemberService : IMemberService
{
    private readonly IMemberRepository _memberRepository;
    private readonly ILogger<MemberService> _logger;
    private readonly IMapper<Member, MemberDTO> _memberMapper;
    private readonly IMapper<Member, MemberRegistrationDTO> _registrationMapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MemberService(IMemberRepository memberRepository, 
        ILogger<MemberService> logger, 
        IMapper<Member, MemberDTO> memberMapper, 
        IMapper<Member, MemberRegistrationDTO> registrationMapper, 
        IHttpContextAccessor httpContextAccessor)
    {
        _memberRepository = memberRepository;
        _logger = logger;
        _memberMapper = memberMapper;
        _registrationMapper = registrationMapper;
        _httpContextAccessor = httpContextAccessor;
    }


    public async Task<bool> DeleteByIdAsync(Guid id)
    {
        throw new NotImplementedException("Will be implemented later");
    }

    public async Task<MemberDTO?> RegistrationAsync(MemberRegistrationDTO registrationDTO)
    {
        var member = _registrationMapper.MapToModel(registrationDTO);
        
        _logger.LogInformation($"trying to add a new member with id: {member.Id}");
        member.Created = DateOnly.FromDateTime(DateTime.UtcNow);
        member.Updated = DateOnly.FromDateTime(DateTime.UtcNow);
        member.HashedPassword = BCrypt.Net.BCrypt.HashPassword(registrationDTO.Password);
        
        var addedMember = await _memberRepository.AddAsync(member);
        if (addedMember is null)
        {
            _logger.LogError("Failed to add member.");
            throw new DataException("Failed to add member.");
        }
        
        return _memberMapper.MapToDTO(addedMember);
    }
    
    public async Task<MemberDTO?> GoogleLoginAsync(ClaimsPrincipal googleUser)
    {
        var firstName = googleUser.FindFirstValue(ClaimTypes.GivenName);
        var lastName = googleUser.FindFirstValue(ClaimTypes.Surname);
        var email = googleUser.FindFirstValue(ClaimTypes.Email);
        var birthYear = googleUser.FindFirstValue(ClaimTypes.DateOfBirth);

        var baseUsername = GenerateBaseUsername(firstName, lastName);
        var uniqueUsername = await EnsureUniqueUsernameAsync(baseUsername);

        var existingMember = await FindMemberByEmailAsync(email);
        if (existingMember != null)
        {
            _logger.LogInformation($"User with email {email} already exists.");
            return existingMember; 
        }

        var newGoogleMember = new Member()
        {
            UserName = uniqueUsername,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            BirthYear = DateOnly.FromDateTime(DateTime.Parse(birthYear))
        };
        
        var newMember = await _memberRepository.AddAsync(newGoogleMember);
        if (newMember is null)
        {
            _logger.LogError("Failed to add member.");
            throw new DataException("Failed to add member.");
        }
        
        return _memberMapper.MapToDTO(newMember);
    }

    public async Task<int?> AuthenticateMemberAsync(int firstName, string password)
    {
        throw new NotImplementedException("Will be implemented later");
    }
    
    
    private string GenerateBaseUsername(string firstName, string lastName)
    {
        if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
        {
            return $"{firstName.ToLower()}.{lastName.ToLower()}";
        }

        if (!string.IsNullOrEmpty(firstName))
        {
            return firstName.ToLower();
        }

        return "googleUser"; 
    }
    
    private async Task<string> EnsureUniqueUsernameAsync(string baseUsername)
    {
        var username = baseUsername;
        var counter = 1;

        while (await _memberRepository.UsernameExistsAsync(username))
        {
            username = $"{baseUsername}{counter}";
            counter++;
        }

        return username;
    }
    
    private async Task<MemberDTO?> FindMemberByEmailAsync(string email)
    {
        var member = await _memberRepository.GetByEmailAsync(email);
        if (member != null)
        {
            return _memberMapper.MapToDTO(member);
        }

        return null;
    }
}