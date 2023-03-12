using Domain.Entities;
using MediatR;

namespace Application.Orders.Create.Commands;

public class CreateOrderNotification : INotification
{
    public Order Order { get; }

    public CreateOrderNotification(Order order)
    {
        Order = order;
    }
}
