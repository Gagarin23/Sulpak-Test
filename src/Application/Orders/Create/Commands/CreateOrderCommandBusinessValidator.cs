using Application.Common.Validators;

namespace Application.Orders.Create.Commands;

public class CreateOrderCommandBusinessValidator : BusinessValidator<CreateOrderCommand>
{
    public CreateOrderCommandBusinessValidator()
    {
        // Валидация в соответствии с доменом.
        // Например, доступны ли товары в регионе заказа
        // или валидация промо-кода
        // и много чего ещё.
        // При неудаче возвращаем ошибку 409 - Conflict.
        // 409 статус код означает, что данные, которые прислал пользователь, корректны с точки зрения ввода,
        // но конфликтуют с текущим состоянием системы.
    }
}
