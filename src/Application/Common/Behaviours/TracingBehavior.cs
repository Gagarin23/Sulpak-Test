using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Common.Behaviours
{
    public class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private static readonly ActivitySource ActivitySource = new ActivitySource(typeof(TRequest).Name);
        
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            using (var activity = ActivitySource.StartActivity())
            {
                try
                {
                    return await next();
                }
                catch (Exception e)
                {
                    activity.SetStatus(ActivityStatusCode.Error, e.ToString());
                    throw;
                }
            }
        }
    }
}
