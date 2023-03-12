using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Orders.Get.Queries;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, List<Order>>
{
    private readonly IDatabaseContext _context;

    public GetOrdersQueryHandler(IDatabaseContext context)
    {
        _context = context;
    }
    
    public async Task<List<Order>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        //Рандомно выбираем заказ
        //Чтобы выборка была быстрая, попадаем в ранее созданный ASC индекс по св-ву Number
        //и вытягиваем первую запись в соответствии с фильтром
        const int startIndex = (int)'A';
        const int endIndex = (int)'Z';

        var firstSymbolOfOrderNumber = (char)Random.Shared.Next(startIndex, endIndex + 1);

        //Пускай случайный запрос будет с ошибкой
        if (Random.Shared.Next(0, 500) == 5)
        {
            throw new Exception("Плохая практика бросать System.Exception");
        }

        return await _context.Orders
            .Where(order => EF.Functions.Like(order.Number, $"{firstSymbolOfOrderNumber}%"))
            .Take(30)
            .ToListAsync(cancellationToken);
    }
}
