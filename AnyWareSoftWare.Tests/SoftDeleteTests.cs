using AnyWareSoftWare.Domain.Entities;
using AnyWareSoftWare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AnyWareSoftWare.Tests
{
    public class SoftDeleteTests
    {
        private static AppDbContext NewContext() =>
            new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        [Fact]
        public async Task Deleting_SoftDeletable_MarksInsteadOfRemoving_AndHidesFromQueries()
        {
            using var ctx = NewContext();
            var user = new ApplicationUser { UserName = "u@x.com", Email = "u@x.com", Name = "U" };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();

            ctx.Users.Remove(user);
            await ctx.SaveChangesAsync();

            Assert.False(await ctx.Users.AnyAsync(u => u.Email == "u@x.com"));

            var stored = await ctx.Users.IgnoreQueryFilters().SingleAsync(u => u.Email == "u@x.com");
            Assert.True(stored.IsDeleted);
        }
    }
}
