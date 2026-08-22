using AnyWareSoftWare.Application.Interfaces;
using AnyWareSoftWare.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AnyWareSoftWare.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ITaskService, TaskService>();

            return services;
        }
    }
}