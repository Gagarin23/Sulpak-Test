using Hangfire.Dashboard;

namespace Infrastructure.Services.Hangfire;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        //в hangfire обязательная авторизация, но для демонстрационных целей опустим её
        return true;
    }
}
