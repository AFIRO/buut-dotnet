using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Xunit.Bookings;

public class FakeDialogService : IDialogService
{
    public IDialogReference Show<TComponent>() where TComponent : IComponent
    {
        throw new NotImplementedException();
    }

    public IDialogReference Show<TComponent>(string? title) where TComponent : IComponent
    {
        throw new NotImplementedException();
    }

    public IDialogReference Show<TComponent>(string? title, DialogOptions options) where TComponent : IComponent
    {
        throw new NotImplementedException();
    }

    public IDialogReference Show<TComponent>(string? title, DialogParameters parameters) where TComponent : IComponent
    {
        throw new NotImplementedException();
    }

    public IDialogReference Show<TComponent>(string? title, DialogParameters parameters, DialogOptions? options) where TComponent : IComponent
    {
        throw new NotImplementedException();
    }

    public IDialogReference Show(Type component)
    {
        throw new NotImplementedException();
    }

    public IDialogReference Show(Type component, string? title)
    {
        throw new NotImplementedException();
    }

    public IDialogReference Show(Type component, string? title, DialogOptions options)
    {
        throw new NotImplementedException();
    }

    public IDialogReference Show(Type component, string? title, DialogParameters parameters)
    {
        throw new NotImplementedException();
    }

    public IDialogReference Show(Type component, string? title, DialogParameters parameters, DialogOptions options)
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowAsync<TComponent>() where TComponent : IComponent
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowAsync<TComponent>(string? title) where TComponent : IComponent
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowAsync<TComponent>(string? title, DialogOptions options) where TComponent : IComponent
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowAsync<TComponent>(string? title, DialogParameters parameters) where TComponent : IComponent
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowAsync<TComponent>(string? title, DialogParameters parameters, DialogOptions? options) where TComponent : IComponent
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowAsync(Type component)
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowAsync(Type component, string? title)
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowAsync(Type component, string? title, DialogOptions options)
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowAsync(Type component, string? title, DialogParameters parameters)
    {
        throw new NotImplementedException();
    }

    public Task<IDialogReference> ShowAsync(Type component, string? title, DialogParameters parameters, DialogOptions options)
    {
        throw new NotImplementedException();
    }

    public IDialogReference CreateReference()
    {
        throw new NotImplementedException();
    }

    private bool? _mockMessageResult;
    public bool? MockMessageResult
    {
        get => _mockMessageResult;
        init => _mockMessageResult = value;
    }

    public Task<Boolean?> ShowMessageBox(string? title, string message, string yesText = "OK", string? noText = null,
        string? cancelText = null, DialogOptions? options = null)
    {
        return Task.FromResult(MockMessageResult);
    }

    public Task<Boolean?> ShowMessageBox(string? title, MarkupString markupMessage, string yesText = "OK", string? noText = null,
        string? cancelText = null, DialogOptions? options = null)
    {
        return Task.FromResult(MockMessageResult);
    }

    public Task<Boolean?> ShowMessageBox(MessageBoxOptions messageBoxOptions, DialogOptions? options = null)
    {
        return Task.FromResult(MockMessageResult);
    }

    public void Close(IDialogReference dialog)
    {
        throw new NotImplementedException();
    }

    public void Close(IDialogReference dialog, DialogResult? result)
    {
        throw new NotImplementedException();
    }

    public event Action<IDialogReference>? OnDialogInstanceAdded;
    public event Action<IDialogReference, DialogResult?>? OnDialogCloseRequested;
}