using System.Collections.Generic;
using System.Diagnostics.Tracing;
using Microsoft.Extensions.Logging;

namespace Api;

public class TelemetryListener : EventListener
{
    private readonly ILogger _logger;

    public TelemetryListener()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger("HttpClient");
        _logger = logger;
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name.Equals("System.Net.Http"))
        {
            EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All);
        }
    }
        
    /*protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        _logger.LogInformation(eventData.Message);
    } */
}
