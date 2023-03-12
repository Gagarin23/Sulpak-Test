using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Common.Options;
using Domain.Constants;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Orders.Processed.Commands;

public class OrdersProcessedCommandHandler : IRequestHandler<OrdersProcessedCommand, Unit>
{
    // READPAST - указывает на то, что запрос должен пропустить заблокированные строки и прочитать только доступные незаблокированные строки.
    // Это позволяет избежать блокировки текущего потока и получить только доступные незаблокированные строки.
    // UPDLOCK - указывает на то, что текущая транзакция получает блокировку на обновление выбранных строк,
    // но при этом другие транзакции могут читать данные из таблицы без блокировки.
    // UPDLOCK нужен, если уровень изоляции установлен по умолчанию НЕ readcommited
    //
    // Далее, перед отправкой на бэкенд дополнительно проверяем,
    // что версия строки, статус которой мы обновили = текущей версии после коммита апдейта.
    // Если версия отличается, то эти данные 'улетели' в другой экземпляр приложения,
    // следовательно, просто их не загружаем в рамках этого запроса.
    // Вероятность того, что с хинтом READPAST мы прочитали заблокированные данные очень низка,
    // НО вероятность выше, если данные шардированы и гео-распределены (когда требуется синхронизация)
    //
    // Зачем индекс? Если данных окажется много, то на мой взгляд, выйграем на join.
    // Зачем вложенный запрос? Join'им по индексу, потом ещё раз итерируемся по пересечению даннных.
    // Почему не поместить [Order].Version = UpdatedOrder.Version в join? Потому что в основной таблице
    // нету индекса id + version.
    // Почему не сделать индекс id + version в основной таблице? Version изменяется при каждой модификации строки,
    // поэтому индекс id + version будет часто фрагментироваться и перестраиваться
    private static readonly FormattableString UnhandledOrdersQuery = @$"
        BEGIN TRANSACTION 

        CREATE TABLE #UpdatedOrder (Id UNIQUEIDENTIFIER, Version BINARY(8))

        UPDATE [Order] WITH (READPAST, UPDLOCK)
        SET StatusId = {OrderStatusConstants.Processing}
        OUTPUT inserted.Id, CAST(inserted.Version AS BINARY(8)) INTO #UpdatedOrder
        WHERE StatusId = {OrderStatusConstants.UnHandled}

        CREATE INDEX I_Id ON #UpdatedOrder (Id) INCLUDE (Version)
        
        SELECT
            Src.*
        FROM (
             SELECT
                [Order].*,
                IIF([Order].Version = UpdatedOrder.Version, 1, 0) IsVersionsEquals
            FROM [Order] WITH (NOLOCK)
            INNER JOIN #UpdatedOrder UpdatedOrder ON [Order].Id = UpdatedOrder.Id
        ) Src
        WHERE IsVersionsEquals = 1
        
        DROP TABLE #UpdatedOrder
        
        COMMIT";

    private readonly IDatabaseContext _context;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public OrdersProcessedCommandHandler(IDatabaseContext context, IServiceScopeFactory serviceScopeFactory)
    {
        _context = context;
        _serviceScopeFactory = serviceScopeFactory;
    }
    
    public async Task<Unit> Handle(OrdersProcessedCommand request, CancellationToken cancellationToken)
    {
        var orders = await GetUnhandledOrdersAsync(cancellationToken);

        if (!orders.Any())
        {
            return Unit.Value;
        }
            
        await Parallel.ForEachAsync
        (
            orders.Chunk(StaticOptions.ParallelOptions.MaxDegreeOfParallelism),
            StaticOptions.ParallelOptions,
            async (orders, cancellationToken) =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                
                var context = scope.ServiceProvider.GetRequiredService<IDatabaseContext>();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var handler = new InnerHandler(context, mediator);

                foreach (var order in orders)
                {
                    await handler.HandleOrderAsync(order, cancellationToken);
                }
            }
        );
        
        return Unit.Value;
    }

    private async ValueTask<List<Order>> GetUnhandledOrdersAsync(CancellationToken cancellationToken)
    {
        return await _context.Orders
            .FromSqlInterpolated(UnhandledOrdersQuery)
            .AsTracking()
            .ToListAsync(cancellationToken);
    }
    
    private class InnerHandler
    {
        private readonly IDatabaseContext _context;
        private readonly IMediator _mediator;

        public InnerHandler(IDatabaseContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }
        
        public async ValueTask HandleOrderAsync(Order order, CancellationToken cancellationToken)
        {
            var someRandomValue = Random.Shared.Next(10, 50);
            
            //имитация работы.
            await Task.Delay(someRandomValue, cancellationToken);

            order.StatusId = someRandomValue % 2 == 0 ?
                OrderStatusConstants.Handled :
                OrderStatusConstants.Error;

            // Тут важно выбирать стратегию обработки.
            // Для примера, преполагается, что обработка заказа вовлекается в долгий пайплайн со сложной бизнес логикой
            // и каждый экземпляр заказа обрабатывается и сохраняется отдельно.
            // Притом вызов SaveChangesAsync может НЕ ОЗНАЧАТЬ сохранение в базу,
            // под капотом мы можем переопределить этот метод чтобы регулировать слои транзакций с помощью SavePoint
            // Другими словами у нас может быть "многослойная" транзакция,
            // а SaveChangesAsync окончально коммитит данные, только если мы находимся на самом верхнем слое.
            // В добавок, нужен механизм точечного отката изменений на уровне приложения, в разрезе одного заказа
            // например, с помощью паттерна сага.
            await _context.SaveChangesAsync(cancellationToken);
            
            // Публикуем событие об обработке заказа. Представим, что обработчики существуют :)
            await _mediator.Publish(new SingleOrderProcessedNotification(order), cancellationToken);
        }
    }
}
