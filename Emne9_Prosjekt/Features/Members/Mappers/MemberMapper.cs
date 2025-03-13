using Emne9_Prosjekt.Features.Common.Interfaces;
using Emne9_Prosjekt.Features.Members.Models;

namespace Emne9_Prosjekt.Features.Members.Mappers;

public class MemberMapper : IMapper<Member, MemberDTO>
{
    public MemberDTO MapToDTO(Member model)
    {
        return new MemberDTO()
        {
            MemberId = model.MemberId,
            GoogleId = model.GoogleId,
            UserName = model.UserName,
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            BirthYear = model.BirthYear,
            Created = model.Created,
            Updated = model.Updated,
        };
    }

    public Member MapToModel(MemberDTO dto)
    {
        return new Member()
        {
            MemberId = dto.MemberId,
            GoogleId = dto.GoogleId,
            UserName = dto.UserName,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            BirthYear = dto.BirthYear,
            Created = dto.Created,
            Updated = dto.Updated
        };
    }
}