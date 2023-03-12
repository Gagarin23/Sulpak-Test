using System;

namespace Domain.Entities;

public class OrderStatus
{
    public Guid Id { get; }
    public string Name { get; }
    
    public OrderStatus(Guid id, string name)
    {
        Id = id;
        Name = name;
    }

    //для EF
    private OrderStatus()
    {
        
    }
}
