using System;
using MediatR;

namespace Application.Orders.Create.Commands;

public class CreateOrderCommand : IRequest<Guid>
{
    public CreateOrderCommand(/*поля заказа*/)
    {
        
    }
}
