using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using MudSmoke.Components;

namespace MudSmoke;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder().UseMauiApp<App>();
        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddMudServices();
        // Renderer errors outside a case's error boundary, such as in popover content, only reach the log.
        builder.Logging.AddProvider(new SmokeLoggerProvider());
        return builder.Build();
    }
}

public sealed class App : Application
{
    protected override Window CreateWindow(IActivationState? activationState)
    {
        var webView = new BlazorWebView { HostPage = "wwwroot/index.html" };
        webView.RootComponents.Add(new RootComponent { Selector = "#app", ComponentType = typeof(Runner) });
        return new Window(new ContentPage { Content = webView }) { Title = "MudBlazor AOT smoke" };
    }
}

public static class Smoke
{
    public static void Log(string phase, int index, string? name, string? detail = null)
    {
        var text = (detail ?? "").Replace('\n', ' ').Replace('\r', ' ').Replace('|', '/');
        if (text.Length > 300)
        {
            text = text[..300];
        }

        Console.WriteLine($"SMOKE|{phase}|{index}|{name}|{text}");
    }

    public static string Describe(Exception? exception) =>
        exception is null ? "" : $"{exception.GetType().FullName}: {exception.Message}";
}

public sealed class SmokeLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new SmokeLogger(categoryName);

    public void Dispose()
    {
    }

    private sealed class SmokeLogger(string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                Smoke.Log("log", Runner.CurrentIndex, category, exception is null ? formatter(state, exception) : Smoke.Describe(exception));
            }
        }
    }
}
