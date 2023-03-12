using System.Collections.Generic;
using Domain.Entities;
using MediatR;

namespace Application.Orders.Get.Queries;

public class GetOrdersQuery : IRequest<List<Order>>
{
    
}
