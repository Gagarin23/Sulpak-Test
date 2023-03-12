using System;
using System.ComponentModel.DataAnnotations;
using Domain.Constants;
using Domain.Exceptions;

namespace Domain.Entities;

//Для простоты опущены системные поля (время создания/обновления),
//поля-связки с другими таблицами (например, позиции заказа)
//и логика отвечающая за доступ и изменение полей.
public class Order
{
    private Guid _id;

    public Guid Id
    {
        get => _id;
        set
        {
            if (_id != Guid.Empty)
            {
                throw new DomainException(BusinessValidationMessages.IdAlreadyExists);
            }
            _id = value;
        }
    }

    public string Number { get; set; }
    public Guid StatusId { get; set; }
    
    [Timestamp]
    public byte[] Version { get; set; }

    public OrderStatus Status { get; set; }
}
