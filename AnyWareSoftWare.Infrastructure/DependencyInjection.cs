using AnyWareSoftWare.Application.Interfaces;
using AnyWareSoftWare.Domain.Entities;
using AnyWareSoftWare.Domain.Interfaces;
using AnyWareSoftWare.Infrastructure.Data;
using AnyWareSoftWare.Infrastructure.Repositories;
using AnyWareSoftWare.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace AnyWareSoftWare.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions => sqlOptions.EnableRetryOnFailure()));

            services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();
            services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));

            services.AddIdentityCore<ApplicationUser>(options =>
                {
                    options.User.RequireUniqueEmail = true;
                    options.Password.RequiredLength = 6;
                })
                .AddRoles<IdentityRole<int>>()
                .AddEntityFrameworkStores<AppDbContext>();

            var redisConn = configuration.GetConnectionString("Redis") ?? "localhost";
            var redisOptions = ConfigurationOptions.Parse(redisConn);
            redisOptions.AbortOnConnectFail = false;
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisOptions));
            services.AddScoped<IRedisCacheService, RedisCacheService>();

            services.AddSingleton<IBackgroundQueue, BackgroundQueue>();
            services.AddHostedService<TaskProcessingWorker>();

            return services;
        }
    }
}