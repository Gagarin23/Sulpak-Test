using System.Threading;
using System.Threading.Tasks;
using Application.Orders.Create.Commands;
using Domain.Entities;
using Hangfire;
using MediatR;

namespace Application.Orders.Create.NotificationHandlers;

public class WarehouseNotificationHandler : INotificationHandler<CreateOrderNotification>
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public WarehouseNotificationHandler(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public Task Handle(CreateOrderNotification notification, CancellationToken cancellationToken)
    {
        // Медиатор, после публикации CreateOrderNotification, запускает все обработчики последовательно.
        // Если хотим асинхронную обработку, с возможностью retry, то ставим задачу в очередь планировщика.
        _backgroundJobClient.Enqueue(() => ReserveProductsAsync(notification.Order, cancellationToken));

        return Task.CompletedTask;
    }

    public async ValueTask ReserveProductsAsync(Order order, CancellationToken cancellationToken)
    {
        /*что-то делаем*/
    }
}
