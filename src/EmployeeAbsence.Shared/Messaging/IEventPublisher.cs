namespace EmployeeAbsence.Shared.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent message, CancellationToken cancellationToken = default) where TEvent : class;
}
