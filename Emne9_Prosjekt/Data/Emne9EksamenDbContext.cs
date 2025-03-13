using Emne9_Prosjekt.Features.Members.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Emne9_Prosjekt.Data;

public class Emne9EksamenDbContext : IdentityDbContext<IdentityUser>
{
    public Emne9EksamenDbContext(DbContextOptions<Emne9EksamenDbContext> options) : base(options)
    {
        
    }
    
    public DbSet<Member> Member { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Member>(entity =>
        {
            entity.HasIndex(m => m.UserName).IsUnique();
        });
    }
}