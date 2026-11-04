using EmployeeAbsence.Shared.Messaging;
using LoggingService.Worker.Data;
using LoggingService.Worker.Messaging;
using LoggingService.Worker.Services;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<LogsDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("LogsDb")));
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection(RabbitMqSettings.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<ProfileUpdatedMessageHandler>();
builder.Services.AddHostedService<RabbitMqConsumer>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LogsDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            await db.Database.MigrateAsync();
            break;
        }
        catch (Exception ex) when (attempt < 10)
        {
            logger.LogWarning(ex, "Database not ready (attempt {Attempt}), retrying in 5s", attempt);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }
}

host.Run();
