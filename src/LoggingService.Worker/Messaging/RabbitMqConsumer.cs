using EmployeeAbsence.Shared.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace LoggingService.Worker.Messaging;

/// <summary>Background service that consumes the activity log queue (manual ack, one message at a time).</summary>
public sealed class RabbitMqConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqSettings> options,
    ILogger<RabbitMqConsumer> logger) : BackgroundService
{
    private readonly RabbitMqSettings _settings = options.Value;
    private IConnection? _connection;
    private IModel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConnectWithRetryAsync(stoppingToken);
        if (_channel is null)
        {
            return;
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, delivery) =>
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<ProfileUpdatedMessageHandler>();
                var result = await handler.HandleAsync(delivery.Body, stoppingToken);

                if (result == HandleResult.Success)
                    _channel.BasicAck(delivery.DeliveryTag, multiple: false);
                else
                    _channel.BasicNack(delivery.DeliveryTag, multiple: false, requeue: false);
            }
            catch (Exception ex)
            {
                // Transient failure (e.g. database down): requeue only if it was not already redelivered once.
                logger.LogError(ex, "Failed to process message {DeliveryTag}", delivery.DeliveryTag);
                _channel.BasicNack(delivery.DeliveryTag, multiple: false, requeue: !delivery.Redelivered);
            }
        };

        _channel.BasicConsume(_settings.QueueName, autoAck: false, consumer);
        logger.LogInformation("Listening on queue {Queue}", _settings.QueueName);

        await Task.Delay(Timeout.Infinite, stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
    }

    private async Task ConnectWithRetryAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _connection = factory.CreateConnection("logging-service-consumer");
                _channel = _connection.CreateModel();
                _channel.QueueDeclare(_settings.QueueName, durable: true, exclusive: false, autoDelete: false);
                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "RabbitMQ not reachable at {Host}:{Port}, retrying in 5s", _settings.HostName, _settings.Port);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ContinueWith(_ => { }, TaskScheduler.Default);
            }
        }
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
