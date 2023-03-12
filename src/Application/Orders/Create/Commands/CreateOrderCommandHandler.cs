using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Constants;
using Domain.Entities;
using Mapster;
using MediatR;

namespace Application.Orders.Create.Commands;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IDatabaseContext _context;
    private readonly IMediator _mediator;

    public CreateOrderCommandHandler(IDatabaseContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }
    
    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        //маппим поля на доменный объект
        var order = request.Adapt<Order>();

        order.StatusId = OrderStatusConstants.UnHandled;
        order.Number = GenerateRandomOrderNumber();

        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        await _mediator.Publish(new CreateOrderNotification(order), cancellationToken);

        return order.Id;
    }

    private string GenerateRandomOrderNumber()
    {
        //просто генирация строки без лишних аллокаций.
        const int startIndex = (int)'A';
        const int endIndex = (int)'Z';
        const int length = 15;
        
        Span<char> span = stackalloc char[length];
        
        for (int i = 0; i < length; i++)
        {
            span[i] = (char)Random.Shared.Next(startIndex, endIndex + 1);
        }

        return span.ToString();
    }
}
