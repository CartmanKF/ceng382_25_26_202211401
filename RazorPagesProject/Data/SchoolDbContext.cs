using Microsoft.EntityFrameworkCore;
using RazorPagesProject.Models;

namespace RazorPagesProject.Data
{
    public class SchoolDbContext : DbContext
    {
        public SchoolDbContext(DbContextOptions<SchoolDbContext> options)
            : base(options)
        {
        }

        public DbSet<Class> Classes { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure global query filter for soft delete
            modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
        }
    }
} 