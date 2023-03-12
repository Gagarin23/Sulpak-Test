using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        //Согласно рекомендациям MS, таблицы должны называться в единственном числе
        builder.ToTable("Order");

        //Создаем последовательный гуид для снижения фрагментации кластерного индекса
        //Если понадобиться шардирование таблиц, можно удалить или заменить на NEWID()
        //Поясню.
        //1. Сам по себе случайный гуид моментально фрагментирует кластерный индекс,
        //делая его последовальным мы избавляемся от этой проблемы  
        //2. Шардирование таблиц с инкрементальным идентификатором, например INT, порождает большие проблемы синхронизации
        //3. Если для повышения производительности, требуется гео-распределённое шардирование таблиц, 
        //то куда проще изменить дефолтное значение у первичного ключа, чем производить миграцию данных из-за смены типа ключа(почему int плох, см. п. 2).
        builder.Property(x => x.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        var numberIndex = builder.HasIndex(x => x.Number)
            .IsUnique();
        
        builder.Property(x => x.Number)
            .HasMaxLength(20)
            .IsRequired();

        SqlServerIndexBuilderExtensions.IncludeProperties
        (
            numberIndex,
            x => new
            {
                x.Id,
                x.StatusId
            }
        );

        //В текущей версии индексы для внешних ключей EF создает автоматически
        //builder.HasIndex(x => x.StatusId);
    }
}
