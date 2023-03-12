using Domain.Constants;
using Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class OrderStatusConfiguration : IEntityTypeConfiguration<OrderStatus>
{
    public void Configure(EntityTypeBuilder<OrderStatus> builder)
    {
        //Согласно рекомендациям MS, таблицы должны называться в единственном числе
        builder.ToTable("OrderStatus");

        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Name)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasData
        (
            new OrderStatus[]
            {
                new OrderStatus(OrderStatusConstants.Handled, "Обработано"),
                new OrderStatus(OrderStatusConstants.UnHandled, "Не обработано"),
                new OrderStatus(OrderStatusConstants.Error, "Ошибка"),
                new OrderStatus(OrderStatusConstants.Processing, "В обработке")
            }
        );
    }
}
