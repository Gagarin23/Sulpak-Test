using Domain.Entities;
using MediatR;

namespace Application.Orders.Processed.Commands;

public class SingleOrderProcessedNotification : INotification
{
    public Order Order { get; }

    public SingleOrderProcessedNotification(Order order)
    {
        Order = order;
    }
}
