using AnyWareSoftWare.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AnyWareSoftWare.Infrastructure.Data
{
    public class AppDbContext
        : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<TaskItem> Tasks { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.Name).IsRequired().HasMaxLength(100);
                entity.HasQueryFilter(u => !u.IsDeleted);
            });

            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.Property(t => t.Title).IsRequired().HasMaxLength(200);
                entity.HasOne(t => t.User)
                      .WithMany(u => u.Tasks)
                      .HasForeignKey(t => t.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasQueryFilter(t => !t.IsDeleted);
            });

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.Property(t => t.Token).IsRequired();
                entity.HasIndex(t => t.Token);
                entity.HasOne(t => t.User)
                      .WithMany(u => u.RefreshTokens)
                      .HasForeignKey(t => t.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasQueryFilter(t => !t.IsDeleted);
            });

            SeedData(modelBuilder);
        }

        public override int SaveChanges()
        {
            ApplyEntityRules();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyEntityRules();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyEntityRules()
        {
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = null;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAt = now;
                }
            }

            foreach (var entry in ChangeTracker.Entries<ApplicationUser>())
            {
                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.UpdatedAt = now;
                }
            }
        }

        private static void SeedData(ModelBuilder modelBuilder)
        {
            const int adminRoleId = 1;
            const int userRoleId = 2;
            const int adminUserId = 1;
            var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<IdentityRole<int>>().HasData(
                new IdentityRole<int> { Id = adminRoleId, Name = "Admin", NormalizedName = "ADMIN", ConcurrencyStamp = "role-admin-stamp" },
                new IdentityRole<int> { Id = userRoleId, Name = "User", NormalizedName = "USER", ConcurrencyStamp = "role-user-stamp" });

            modelBuilder.Entity<ApplicationUser>().HasData(new ApplicationUser
            {
                Id = adminUserId,
                Name = "Admin",
                UserName = "admin@example.com",
                NormalizedUserName = "ADMIN@EXAMPLE.COM",
                Email = "admin@example.com",
                NormalizedEmail = "ADMIN@EXAMPLE.COM",
                EmailConfirmed = true,
                PasswordHash = "AQAAAAIAAYagAAAAEBOcXCzRI1i15rKTbdp1CwF8K93y14RsU2bg2GkrM0ix/1gZkOHJ5vJ5BJj1a1umgg==",
                SecurityStamp = "admin-security-stamp",
                ConcurrencyStamp = "admin-concurrency-stamp",
                CreatedAt = seedDate,
                IsDeleted = false
            });

            modelBuilder.Entity<IdentityUserRole<int>>().HasData(
                new IdentityUserRole<int> { UserId = adminUserId, RoleId = adminRoleId });
        }
    }
}
