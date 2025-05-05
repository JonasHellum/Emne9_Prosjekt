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

    /// <summary>
    /// Adds a new member entity to the database.
    /// </summary>
    /// <param name="entity">The member entity to be added.</param>
    /// <returns>The added member entity if successfully saved, otherwise null.</returns>
    public async Task<Member?> AddAsync(Member entity)
    {
        _logger.LogDebug($"Adding new member with Id: {entity.MemberId}, " +
                         $"UserName: {entity.UserName}, FirstName: {entity.FirstName}," +
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

    /// <summary>
    /// Updates an existing member entity in the database with the provided values.
    /// </summary>
    /// <param name="entity">The member entity containing updated values.</param>
    /// <returns>The updated member entity if the operation is successful and the member exists; otherwise, null.</returns>
    public async Task<Member?> UpdateAsync(Member entity)
    {
        _logger.LogDebug($"Finding member based on id: {entity.MemberId}");
        var member = await _dbContext.Member.FirstOrDefaultAsync(m => m.MemberId == entity.MemberId);
        if (member == null) return null;

        _logger.LogDebug($"Updating member with id: {entity.MemberId} with current values: " +
                         $"from: {member.UserName} to: {entity.UserName} " +
                         $"from: {member.FirstName} to: {entity.FirstName} " +
                         $"from {member.LastName} to: {entity.LastName} " +
                         $"from: {member.Email} to: {entity.Email} " +
                         $"from: {member.BirthYear} to: {entity.BirthYear} " +
                         $"from: {member.Updated} to: {entity.Updated}");
        
        _dbContext.Member.Update(member);
        await _dbContext.SaveChangesAsync();
        
        _logger.LogInformation($"Updated member with id {member.MemberId}");
        return member;
    }

    /// <summary>
    /// Retrieves a member entity by its unique identifier.
    /// </summary>
    /// <param name="memberId">The unique identifier of the member to be retrieved.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the member entity if found, otherwise null.</returns>
    public async Task<Member?> GetByIdAsync(Guid memberId)
    {
        _logger.LogDebug($"Retrieving member with id: {memberId} from database.");
        return await _dbContext.Member.FirstOrDefaultAsync(m => m.MemberId == memberId);
    }

    /// <summary>
    /// Finds a collection of member entities that match the specified predicate.
    /// </summary>
    /// <param name="predicate">An expression used to filter the member entities.</param>
    /// <returns>A collection of member entities that satisfy the predicate.</returns>
    public async Task<IEnumerable<Member>> FindAsync(Expression<Func<Member, bool>> predicate)
    {
        _logger.LogDebug($"Finding members based on predicate: {predicate}");
        return await _dbContext.Member
            .Where(predicate)
            .ToListAsync();
    }

    /// <summary>
    /// Checks if a username already exists in the database.
    /// </summary>
    /// <param name="username">The username to be checked.</param>
    /// <returns>A boolean indicating whether the username exists.</returns>
    public async Task<bool> UserNameExistsAsync(string username)
    {
        _logger.LogDebug($"Checking if username {username} exists in database.");
        return await _dbContext.Member.AnyAsync(m => m.UserName.ToLower() == username.ToLower());
    }

    /// <summary>
    /// Retrieves a member entity from the database based on the provided email address.
    /// </summary>
    /// <param name="email">The email address of the member to retrieve.</param>
    /// <returns>The member entity if found, otherwise null.</returns>
    public async Task<Member?> GetByEmailAsync(string email)
    {
        _logger.LogDebug($"Retrieving member with email {email} from database.");
        return await _dbContext.Member.FirstOrDefaultAsync(m => m.Email.ToLower() == email.ToLower());
    }

    /// <summary>
    /// Checks if an email already exists in the database.
    /// </summary>
    /// <param name="email">The email address to check for existence.</param>
    /// <returns>True if the email exists, otherwise false.</returns>
    public async Task<bool> EmailExistsAsync(string email)
    {
        _logger.LogDebug($"Checking if email {email} exists in database.");
        return await _dbContext.Member.AnyAsync(m => m.Email.ToLower() == email.ToLower());
    }

    
    #region For Refresh Tokens

    /// <summary>
    /// Saves a refresh token for a member entity into the database.
    /// </summary>
    /// <param name="refreshToken">The refresh token entity to be saved.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SaveRefreshTokenAsync(MemberRefreshToken refreshToken)
    {
        _logger.LogDebug($"Saving refresh token for member with id: {refreshToken.MemberId} and token: {refreshToken.Token}");
        _dbContext.MemberRefreshToken.Add(refreshToken);
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Validates a refresh token to ensure it is valid, not revoked, and not expired.
    /// </summary>
    /// <param name="token">The refresh token to be validated.</param>
    /// <returns>The matching <see cref="MemberRefreshToken"/> object if the token is valid, otherwise null.</returns>
    public async Task<MemberRefreshToken> ValidateRefreshTokenAsync(string token)
    {
        _logger.LogDebug($"Validating refresh token: {token}");
        return await _dbContext.MemberRefreshToken
            .FirstOrDefaultAsync(t => t.Token == token && !t.Revoked && t.Expires > DateTime.UtcNow);
    }

    /// <summary>
    /// Revokes a refresh token by marking it as revoked in the database.
    /// </summary>
    /// <param name="token">The refresh token to revoke.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task RevokeRefreshTokenAsync(string token)
    {
        _logger.LogDebug($"Revoking refresh token: {token}");
        var refreshToken = await _dbContext.MemberRefreshToken.FirstOrDefaultAsync(t => t.Token == token);
        if (refreshToken != null)
        {
            Console.WriteLine("REVOKE REFRESH TOKEN!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            refreshToken.Revoked = true;
            _dbContext.MemberRefreshToken.Update(refreshToken);
            await _dbContext.SaveChangesAsync();
        }
        Console.WriteLine("Didn't find the refreshtoken for some fucked up raeson??????????????????????????????????????????????????????????");
    }

    /// <summary>
    /// Retrieves a specific refresh token from the database.
    /// </summary>
    /// <param name="token">The refresh token string to look up.</param>
    /// <returns>The matching refresh token entity if found, otherwise null.</returns>
    public async Task<MemberRefreshToken> GetStoredRefreshTokenAsync(string token)
    {
        _logger.LogDebug($"Retrieving refresh token: {token}");
        return await _dbContext.MemberRefreshToken
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }
    #endregion
}