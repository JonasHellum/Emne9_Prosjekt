using System.Linq.Expressions;
using Emne9_Prosjekt.Data;
using Emne9_Prosjekt.Features.Members.Interfaces;
using Emne9_Prosjekt.Features.Members.Models;
using Microsoft.EntityFrameworkCore;

namespace Emne9_Prosjekt.Features.Members;

public class MemberRepository : IMemberRepository
{
    private readonly ILogger<MemberRepository> _logger;
    private readonly Emne9EksamenDbContext _dbContext;

    public MemberRepository(ILogger<MemberRepository> logger, 
        Emne9EksamenDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }
    public async Task<Member?> AddAsync(Member entity)
    {
        _logger.LogDebug($"Adding new member with Id: {entity.MemberId}, " +
                         $"UserName: {entity.UserName}FirstName: {entity.FirstName}," +
                         $"LastName: {entity.LastName}, BirthYear: {entity.BirthYear}," +
                         $"Created: {entity.Created}, Updated: {entity.Updated}");
        await _dbContext.Member.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation($"Added new member with Id: {entity.MemberId}");
        return entity;
    }

    public async Task<Member?> DeleteByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }
    
    public async Task<Member?> UpdateAsync(Member entity)
    {
        throw new NotImplementedException("Will be implemented later");
    }

    public async Task<Member?> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException("Will be implemented later");
    }

    public async Task<IEnumerable<Member>> FindAsync(Expression<Func<Member, bool>> predicate)
    {
        throw new NotImplementedException("Will be implemented later");
    }

    public async Task<IEnumerable<Member>> GetPagedAsync(int pageNumber, int pageSize)
    {
        throw new NotImplementedException("Will be implemented later");
    }
    
    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await _dbContext.Member.AnyAsync(m => m.UserName.ToLower() == username.ToLower());
    }
    
    public async Task<Member?> GetByEmailAsync(string email)
    {
        return await _dbContext.Member.FirstOrDefaultAsync(m => m.Email == email);
    }


}