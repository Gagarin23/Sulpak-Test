using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Options;
using Application.Orders.Create.Commands;
using Application.Orders.Get.Queries;
using Application.Orders.Processed.Commands;
using Hangfire;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Services.Hangfire
{
    public static class HangfireJobScheduler
    {
        private static readonly CancellationTokenSource _cts = new CancellationTokenSource();
        
        public static void ScheduleRecurringJobs()
        {
            //Регестрируем джобы с запуском каждую минуту
            RecurringJob.AddOrUpdate<OrdersHandlerJob>
            (
                nameof(OrdersHandlerJob),
                job => job.RunAsync(_cts.Token),
                Cron.Never,
                TimeZoneInfo.Local
            );
            
            RecurringJob.AddOrUpdate<OrdersWriterJob>
            (
                nameof(OrdersWriterJob),
                job => job.RunAsync(_cts.Token),
                Cron.Never,
                TimeZoneInfo.Local
            );
            
            RecurringJob.AddOrUpdate<OrdersReaderJob>
            (
                nameof(OrdersReaderJob),
                job => job.RunAsync(_cts.Token),
                Cron.Never,
                TimeZoneInfo.Local
            );
        }
    }
    
    //Обычно, джобы храняться в отдельной папке и имееют свою, иногда сложную, инфрастуктурную логику
    //Для упрощения и наглядности сгруппировал все джобы здесь.

    public class OrdersHandlerJob
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public OrdersHandlerJob(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async ValueTask RunAsync(CancellationToken cancellationToken)
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            
            //запросы не чаще 1 секунды
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                //отправляем в параллельную обработку с ожиданием
                //т.к. без ожидания могут закончиться подключения в пулле
                await Task.Run
                (
                    async () =>
                    {
                        using var scope = _scopeFactory.CreateScope();
                        
                        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                        await mediator.Send(new OrdersProcessedCommand(), cancellationToken);
                    },
                    cancellationToken
                );
            }
        }
    }

    public class OrdersWriterJob
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public OrdersWriterJob(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async ValueTask RunAsync(CancellationToken cancellationToken)
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            var ordersPerSecond = 5;

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                //отправляем в параллельную обработку с ожиданием
                //т.к. без ожидания могут закончиться подключения в пулле
                await Parallel.ForEachAsync
                (
                    Enumerable.Range(0, ordersPerSecond).Select(_ => new CreateOrderCommand()),
                    StaticOptions.ParallelOptions!,
                    async (command, cancellationToken) =>
                    {
                        using var scope = _scopeFactory.CreateScope();
                    
                        var mediator = scope.ServiceProvider.GetService<IMediator>();
                        await mediator.Send(command, cancellationToken);
                    }
                );
            }
        }
    }

    public class OrdersReaderJob
    {
        private readonly IServiceScopeFactory _scopeFactory;
        
        public OrdersReaderJob(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async ValueTask RunAsync(CancellationToken cancellationToken)
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            var requests = 50;

            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                //отправляем в параллельную обработку с ожиданием
                //т.к. без ожидания могут закончиться подключения в пулле
                await Parallel.ForEachAsync
                (
                    Enumerable.Range(0, requests).Select(_ => new GetOrdersQuery()),
                    StaticOptions.ParallelOptions!,
                    async (command, cancellationToken) =>
                    {
                        try
                        {
                            using var scope = _scopeFactory.CreateScope();
                    
                            var mediator = scope.ServiceProvider.GetService<IMediator>();
                            await mediator.Send(command, cancellationToken);
                        }
                        catch
                        {
                            //имитируем независимые запросы,
                            //если упадёт один запрос (в коде есть умышленный exception),
                            //то упадёт весь Parallel.ForEachAsync
                            //в любом случае, все ошибки залогируются с трассировкой
                        }
                    }
                );
            }
        }
    }
}
