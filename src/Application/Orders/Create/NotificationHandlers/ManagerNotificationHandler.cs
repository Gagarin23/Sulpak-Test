using System.Threading;
using System.Threading.Tasks;
using Application.Orders.Create.Commands;
using Domain.Entities;
using Hangfire;
using MediatR;

namespace Application.Orders.Create.NotificationHandlers;

public class ManagerNotificationHandler : INotificationHandler<CreateOrderNotification>
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public ManagerNotificationHandler(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }
    
    public Task Handle(CreateOrderNotification notification, CancellationToken cancellationToken)
    {
        // Медиатор, после публикации CreateOrderNotification, запускает все обработчики последовательно.
        // Если хотим асинхронную обработку, с возможностью retry, то ставим задачу в очередь планировщика.
        _backgroundJobClient.Enqueue(() => NotifyManagerAsync(notification.Order, cancellationToken));

        return Task.CompletedTask;
    }

    public async ValueTask NotifyManagerAsync(Order order, CancellationToken cancellationToken)
    {
        /*что-то делаем*/
    }
}
