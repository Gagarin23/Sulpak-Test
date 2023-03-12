using Api.Filters;
using Application;
using Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json.Serialization;
using Hangfire;
using Hangfire.SqlServer;
using Infrastructure.Persistence;
using Infrastructure.Services.Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace Api
{
    public class Program
    {
        private static ConfigurationManager _configuration;

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            BuildConfiguration(builder.Configuration);
            ConfigureServices(builder.Services);

            var app = builder.Build();
            ConfigureApplication(app);

            MigrateDatabase(app);
            ConfigureHangfire(app);

            app.Run();
        }
        
        private static void BuildConfiguration(ConfigurationManager configuration)
        {
            configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile
                (
                    "appsettings.json",
                    optional: false,
                    reloadOnChange: true
                )
                .AddJsonFile
                (
                    $"appsettings.{EnvironmentExtension.CurrentEnvironment}.json",
                    optional: true,
                    reloadOnChange: true
                )
                .Build();

            _configuration = configuration;
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers
                (
                    options => options.Filters.Add<ApiExceptionFilter>()
                )
                .AddJsonOptions
                (
                    options =>
                    {
                        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                        options.JsonSerializerOptions.WriteIndented = true;
                    }
                );

            services.AddApplication();

            services.ConfigureSettings(_configuration);

            services.AddInfrastructure(_configuration);

            services.AddCors();

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen
            (
                options =>
                {
                    options.SwaggerDoc
                    (
                        "v1",
                        new OpenApiInfo
                        {
                            Version = "v1",
                            Title = "API",
                            Description = "An ASP.NET Core Web API",
                        }
                    );

                    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
                }
            );

            services.AddOptions();
            services.AddTracing(_configuration);
        }

        private static void ConfigureApplication(WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors
            (
                builder =>
                {
                    builder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                }
            );

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }   
        
        private static void ConfigureHangfire(WebApplication app)
        {
            // Экземпляр панели управления Hangfire
            var apiStorage = new SqlServerStorage(
                app.Configuration.GetConnectionString("DefaultConnection"),
                new SqlServerStorageOptions
                {
                    SchemaName = "Hangfire"
                });
            app.UseHangfireDashboard
            (
                options: new DashboardOptions()
                {
                    Authorization = new[] { new HangfireAuthorizationFilter() }
                },
                storage: apiStorage
            );
            
            HangfireJobScheduler.ScheduleRecurringJobs();
        }

        private static void MigrateDatabase(WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider
                    .GetRequiredService<IDbContextFactory<DatabaseContext>>()
                    .CreateDbContext();

                db.Database.Migrate();
            }
        }
    }
}
