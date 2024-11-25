using Microsoft.Extensions.DependencyInjection;
using Rise.Services.Events;

public class GenericEventHandler<TEvent> : IEventHandler<TEvent>
    where TEvent : class, IEvent
{
    private readonly IServiceProvider _serviceProvider;

    public GenericEventHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task HandleAsync(TEvent eventArgs)
    {
        // Logic to handle the event, e.g., logging or dispatching specific actions
        Console.WriteLine($"Handling event of type {typeof(TEvent).Name}");

        // Use scoped services if needed
        using var scope = _serviceProvider.CreateScope();
        // Resolve and handle the event as needed
    }
}
