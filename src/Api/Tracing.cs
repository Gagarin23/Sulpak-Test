using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Application;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Api
{
    public class TracingConfig
    {
        public bool Enabled { get; set; }
        public string ServiceName { get; set; }
        public string JaegerEndpoint { get; set; }
        public string AgentHost { get; set; }
        public int AgentPort { get; set; }
        public string Protocol { get; set; }
    }

    public class TracingInterceptorConfig
    {
        public string SourceName { get; }
        public List<Tag> Tags { get; }

        public TracingInterceptorConfig(string sourceName)
        {
            SourceName = sourceName;
            Tags = new List<Tag>();
        }

        public class Tag
        {
            public string Name { get; }
            public string Value { get; }
            public Tag(string name, string value)
            {
                Name = name;
                Value = value;
            }
        }
    }

    public static class TracingExtensions
    {
        public static IServiceCollection AddTracing(this IServiceCollection services, IConfiguration configuration)
        {
            var tracingSection = configuration.GetSection("Tracing");

            var config = new TracingConfig();
            tracingSection.Bind(config);

            if (!config.Enabled)
                return services;

            services.AddOpenTelemetry()
                .WithTracing(builder =>
                    {
                        builder
                            //настройка телеметрии в рамках экземпляра приложения
                            .AddAspNetCoreInstrumentation(config => { config.RecordException = true; })
                            .AddHttpClientInstrumentation(config => { config.RecordException = true; })
                            .AddSource(Assembly.GetAssembly(typeof(AssemblyMark))
                                .GetTypes()
                                .Where(type => 
                                    type.GetInterfaces()
                                        .Any(@interface => @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IRequest<>)))
                                .Select(type => type.Name)
                                .ToArray()
                            )
                            .SetResourceBuilder
                            (
                                ResourceBuilder
                                    .CreateDefault()
                                    .AddService(config.ServiceName)
                                    .AddTelemetrySdk()
                            );

                        if (!string.IsNullOrEmpty(config.JaegerEndpoint))
                        {
                            builder.AddJaegerExporter
                            (
                                opt =>
                                {
                                    switch (config.Protocol)
                                    {
                                        case "http":
                                            opt.Protocol = JaegerExportProtocol.HttpBinaryThrift;
                                            opt.Endpoint = new Uri(config.JaegerEndpoint);
                                            break;
                                        
                                        case "udp":
                                            opt.Protocol = JaegerExportProtocol.UdpCompactThrift;
                                            opt.AgentHost = config.AgentHost;
                                            opt.AgentPort = config.AgentPort;
                                            break;
                                        
                                        default:
                                            throw new Exception("Не задан протокол");
                                    }
                                }
                            );
                        }
                    }
                );
            
            var activityListener = new ActivityListener
            {
                ShouldListenTo = s => true,
                SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivitySamplingResult.AllData,
                Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(activityListener);

            return services;
        }
    }
}
