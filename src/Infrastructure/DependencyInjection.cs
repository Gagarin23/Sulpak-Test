using System;
using Application.Common.Interfaces;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.SqlServer;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var defaultDatabaseConnection = configuration.GetConnectionString("DefaultConnection");
            
            services.AddEntityFrameworkSqlServer();
            services.AddPooledDbContextFactory<DatabaseContext>
            (
                (provider, options) =>
                {
                    options.UseSqlServer
                    (
                        defaultDatabaseConnection,
                        b =>
                            b.MigrationsAssembly(typeof(DatabaseContext).Assembly.FullName)
                    );
                    options.UseInternalServiceProvider(provider);
                    if (EnvironmentExtension.IsDevelopment)
                    {
                        options.EnableSensitiveDataLogging();
                        options.LogTo(Console.WriteLine, LogLevel.Warning);
                    }
                },
                (int)Math.Pow(2, 16)
            );

            services.AddScoped<IDatabaseContext>
            (
                provider => provider.GetRequiredService<IDbContextFactory<DatabaseContext>>().CreateDbContext()
            );
            
            GlobalConfiguration.Configuration.UseRecommendedSerializerSettings
            (
                serializerSettings =>
                {
                    serializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                }
            );

            services.AddHangfire
            (
                config => config
                    .UseSqlServerStorage
                    (
                        defaultDatabaseConnection,
                        new SqlServerStorageOptions
                        {
                            UseRecommendedIsolationLevel = true,
                            DisableGlobalLocks = true,
                            SchemaName = "HangFire"
                        }
                    )
            );
            
            services.AddHangfireServer();

            return services;
        }
    }
}
