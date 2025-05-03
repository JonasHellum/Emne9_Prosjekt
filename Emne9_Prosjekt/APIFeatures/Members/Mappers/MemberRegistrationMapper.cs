using Emne9_Prosjekt.Features.Common.Interfaces;
using Emne9_Prosjekt.Features.Members.Models;

namespace Emne9_Prosjekt.Features.Members.Mappers;

public class MemberRegistrationMapper : IMapper<Member, MemberRegistrationDTO>
{
    public MemberRegistrationDTO MapToDTO(Member model)
    {
        return new MemberRegistrationDTO()
        {
            UserName = model.UserName,
            FirstName = model.FirstName,
            LastName = model.LastName,
            BirthYear = model.BirthYear,
            Email = model.Email,
        };
    }

    public Member MapToModel(MemberRegistrationDTO dto)
    {
        return new Member()
        {
            UserName = dto.UserName,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            BirthYear = dto.BirthYear,
            Email = dto.Email,
        };
    }
}