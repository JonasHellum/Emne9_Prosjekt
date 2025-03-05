using Emne9_Prosjekt.Features.Members.Models;
using Microsoft.EntityFrameworkCore;

namespace Emne9_Prosjekt.Data;

public class Emne9EksamenDbContext : DbContext
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