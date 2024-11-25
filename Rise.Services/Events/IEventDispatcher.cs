namespace Rise.Services.Events;

/// <summary>
/// Dispatches events to their respective handlers.
/// </summary>
public interface IEventDispatcher
{
    Task DispatchAsync<TEvent>(TEvent @event) where TEvent : IEvent;
}
