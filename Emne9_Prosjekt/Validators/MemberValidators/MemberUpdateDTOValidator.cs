﻿using Emne9_Prosjekt.Features.Members.Interfaces;
using Emne9_Prosjekt.Features.Members.Models;
using Emne9_Prosjekt.Validators.Interfaces;
using FluentValidation;

namespace Emne9_Prosjekt.Validators.MemberValidators;

public class MemberUpdateDTOValidator : AbstractValidator<MemberUpdateDTO>
{
    public MemberUpdateDTOValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().WithMessage("Username is required")
            .Length(2, 50).WithMessage("Username must be between 2 and 50 characters");
        
        // RuleFor(x => x.FirstName).NotEmpty().WithMessage("First name is required")
        //     .Length(2, 30).WithMessage("First name must be between 2 and 30 characters");
        //
        // RuleFor(x => x.LastName).NotEmpty().WithMessage("Last name is required")
        //     .Length(2, 50).WithMessage("Last name must be between 2 and 50 characters");
        //
        // RuleFor(x => x.BirthYear)
        //     .Must(birthDate => birthDate.Year >= 1000 && birthDate.Year <= 9999)
        //     .WithMessage("Birth year must be between 1000 and 9999");
        
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email is not valid");
        
        // Optional but validated if provided
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .Length(8, 32).WithMessage("Password must be between 8 and 32 characters")
            .Matches("[0-9]+").WithMessage("Invalid password, need at least 1 number")
            .Matches("[A-Z]+").WithMessage("Invalid password, need at least 1 capital letter")
            .Matches("[a-z]+").WithMessage("Invalid password, need at least 1 lowercase letter")
            .Must(password => !password.Any(c => "æøåÆØÅ"
                .Contains(c))).WithMessage("Password contains invalid characters.'(æ ø å Æ Ø Å)'")
            .When(x => !string.IsNullOrWhiteSpace(x.Password));
    }
}

public class AsyncMemberUpdateDTOValidator : AbstractValidator<MemberUpdateDTO>, IAsyncMemberUpdateValidator
{
    private readonly IMemberRepository _memberRepository;
    
    public AsyncMemberUpdateDTOValidator(IMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
        
        RuleFor(x => x.UserName)
            .MustAsync(
                async (dto, username, _) =>
                {
                    var existingMember = await _memberRepository.GetByEmailAsync(dto.Email);
                    if (existingMember == null || existingMember.UserName == username)
                    {
                        return true;
                    }
                    
                    return !await _memberRepository.UserNameExistsAsync(username);
                })
            .WithMessage("Username already exists, choose another one");
        
        RuleFor(x => x.Email)
            .MustAsync(
                async (dto, email, _) =>
                {
                    var existingMember = await _memberRepository.GetByEmailAsync(dto.Email);
                    if (existingMember == null || existingMember.Email == email)
                    {
                        return true;
                    }
                    return !await _memberRepository.EmailExistsAsync(email);
                })
            .WithMessage("Email already exists, choose another one");
    }
}