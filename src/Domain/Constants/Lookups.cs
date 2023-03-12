using System;

namespace Domain.Constants;

public static class OrderStatusConstants
{
    public static readonly Guid Handled = new Guid("60ecf8c9-f595-4eed-aa76-05b7966b07c9");
    public static readonly Guid UnHandled = new Guid("61ecf8c9-f595-4eed-aa76-05b7966b07c9");
    public static readonly Guid Error = new Guid("62ecf8c9-f595-4eed-aa76-05b7966b07c9");
    public static readonly Guid Processing = new Guid("63ecf8c9-f595-4eed-aa76-05b7966b07c9");
}
