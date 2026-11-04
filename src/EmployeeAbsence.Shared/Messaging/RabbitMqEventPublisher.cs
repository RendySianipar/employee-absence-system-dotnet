using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EmployeeAbsence.Shared.Messaging;

/// <summary>
/// Publishes events as persistent JSON messages to a durable RabbitMQ queue.
/// The connection is opened lazily and reused (register as a singleton).
/// </summary>
public sealed class RabbitMqEventPublisher(IOptions<RabbitMqSettings> options, ILogger<RabbitMqEventPublisher> logger)
    : IEventPublisher, IDisposable
{
    private readonly RabbitMqSettings _settings = options.Value;
    private readonly object _lock = new();
    private IConnection? _connection;
    private IModel? _channel;

    public Task PublishAsync<TEvent>(TEvent message, CancellationToken cancellationToken = default) where TEvent : class
    {
        var body = MessageSerializer.Serialize(message);

        lock (_lock)
        {
            var channel = GetChannel();
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.Type = typeof(TEvent).Name;

            channel.BasicPublish(exchange: string.Empty, routingKey: _settings.QueueName, basicProperties: properties, body: body);
        }

        logger.LogInformation("Published {EventType} to queue {Queue}", typeof(TEvent).Name, _settings.QueueName);
        return Task.CompletedTask;
    }

    private IModel GetChannel()
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        _connection?.Dispose();
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
        };
        _connection = factory.CreateConnection("employee-service-publisher");
        _channel = _connection.CreateModel();
        _channel.QueueDeclare(_settings.QueueName, durable: true, exclusive: false, autoDelete: false);
        return _channel;
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
