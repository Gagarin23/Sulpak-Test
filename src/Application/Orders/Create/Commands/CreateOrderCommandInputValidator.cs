using Application.Common.Validators;

namespace Application.Orders.Create.Commands;

public class CreateOrderCommandInputValidator : InputValidator<CreateOrderCommand>
{
    public CreateOrderCommandInputValidator()
    {
        // Валидация на корретность ввода
        // При неудаче, ошибки агрегируются и возвращаются с кодом 400
        // RuleFor(x => x)
        //     ...
    }
}
