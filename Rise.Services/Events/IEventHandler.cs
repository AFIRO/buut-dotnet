namespace Rise.Services.Events;

/// <summary>
/// Interface for handling events of a specific type.
/// </summary>
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    Task HandleAsync(TEvent @event);
}
