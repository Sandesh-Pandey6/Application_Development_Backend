using Autopartspro.Application.Interfaces;
using Autopartspro.Application.Options;
using Autopartspro.Infrastructure.Data;
using Autopartspro.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Autopartspro.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=autopartspro;Username=postgres;Password=postgres";

        services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(connectionString));

        return services;
    }

    public static IServiceCollection AddImageStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CloudinarySettings>(configuration.GetSection("Cloudinary"));

        services.AddSingleton<IImageStorageService>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<CloudinarySettings>>().Value;
            if (settings.IsConfigured)
                return new CloudinaryImageStorageService(sp.GetRequiredService<IOptions<CloudinarySettings>>());

            return new LocalImageStorageService(sp.GetRequiredService<IWebHostEnvironment>());
        });

        services.AddScoped<IUserProfileImageService, UserProfileImageService>();
        return services;
    }
}
