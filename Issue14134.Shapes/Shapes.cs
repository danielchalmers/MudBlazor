using System.Globalization;
using System.Numerics;

namespace Issue14134;

// Mirrors MudBlazor's ParameterState<T>: abstract base with a non-async override.
public abstract class StateBase<T>
{
    public abstract T Value { get; }

    public abstract Task SetValueAsync(T value);
}

public sealed class State<T> : StateBase<T>
{
    private T _value = default!;

    public override T Value => _value;

    public override Task SetValueAsync(T value)
    {
        if (!EqualityComparer<T>.Default.Equals(_value, value))
        {
            _value = value;
        }

        return Task.CompletedTask;
    }
}

// Blazor holds components through a non-generic base and creates them from a Type.
public abstract class HolderBase
{
    public abstract string ValueText { get; }

    public abstract Func<string?, Task> CreateSetter();
}

// Same shape as MudSlider<T>.SetValueTextAsync in 9.12.0.
public sealed class SvmAsyncHolder<T> : HolderBase where T : struct, INumber<T>
{
    private readonly State<T> _valueState = new();
    private readonly State<T?> _nullableValueState = new();

    public override string ValueText => _valueState.Value.ToString(null, CultureInfo.InvariantCulture);

    public override Func<string?, Task> CreateSetter() => SetValueTextAsync;

    private async Task SetValueTextAsync(string? text)
    {
        if (T.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            await _valueState.SetValueAsync(result);
            await _nullableValueState.SetValueAsync(result);
        }
    }
}

// Still async over T, but parses without a static abstract call.
public sealed class NoSvmAsyncHolder<T> : HolderBase where T : struct, INumber<T>
{
    private readonly State<T> _valueState = new();
    private readonly State<T?> _nullableValueState = new();

    public override string ValueText => _valueState.Value.ToString(null, CultureInfo.InvariantCulture);

    public override Func<string?, Task> CreateSetter() => SetValueTextAsync;

    private async Task SetValueTextAsync(string? text)
    {
        if (text is not null)
        {
            var result = (T)Convert.ChangeType(text, typeof(T), CultureInfo.InvariantCulture);
            await _valueState.SetValueAsync(result);
            await _nullableValueState.SetValueAsync(result);
        }
    }
}

// Candidate fix 1: static abstract parse, no async state machine over T.
public sealed class SvmSyncHolder<T> : HolderBase where T : struct, INumber<T>
{
    private readonly State<T> _valueState = new();
    private readonly State<T?> _nullableValueState = new();

    public override string ValueText => _valueState.Value.ToString(null, CultureInfo.InvariantCulture);

    public override Func<string?, Task> CreateSetter() => SetValueText;

    private Task SetValueText(string? text)
    {
        if (!T.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
        {
            return Task.CompletedTask;
        }

        var first = _valueState.SetValueAsync(result);

        return first.IsCompletedSuccessfully ? _nullableValueState.SetValueAsync(result) : ThenSetNullable(first, result);
    }

    // Separate method so the closure is only allocated on the slow path.
    private Task ThenSetNullable(Task first, T value) => TaskChain.ThenAsync(first, () => _nullableValueState.SetValueAsync(value));
}

// Candidate fixes 1 and 2 together: no static abstract call and no async state machine over T.
public sealed class NoSvmSyncHolder<T> : HolderBase where T : struct, INumber<T>
{
    private readonly State<T> _valueState = new();
    private readonly State<T?> _nullableValueState = new();

    public override string ValueText => _valueState.Value.ToString(null, CultureInfo.InvariantCulture);

    public override Func<string?, Task> CreateSetter() => SetValueText;

    private Task SetValueText(string? text)
    {
        if (text is null)
        {
            return Task.CompletedTask;
        }

        var result = (T)Convert.ChangeType(text, typeof(T), CultureInfo.InvariantCulture);
        var first = _valueState.SetValueAsync(result);

        return first.IsCompletedSuccessfully ? _nullableValueState.SetValueAsync(result) : ThenSetNullable(first, result);
    }

    private Task ThenSetNullable(Task first, T value) => TaskChain.ThenAsync(first, () => _nullableValueState.SetValueAsync(value));
}

// Non-generic, so its state machine has no T.
public static class TaskChain
{
    public static async Task ThenAsync(Task first, Func<Task> next)
    {
        await first;
        await next();
    }
}

// Mirrors EventCallbackFactoryBinderExtensions.CreateBinderCoreAsync: an async closure in a generic method, invoked through DynamicInvoke.
public static class Binder
{
    public static Delegate Create<TValue>(Func<TValue, Task> setter, Func<object?, TValue> convert)
    {
        async Task Callback(object? value)
        {
            await setter(convert(value));
        }

        return (Func<object?, Task>)Callback;
    }
}

public static class Cases
{
    public static IReadOnlyList<string> Names { get; } =
    [
        "svm-async-int",
        "svm-async-double",
        "svm-async-int-direct",
        "nosvm-async-int",
        "svm-sync-int",
        "nosvm-sync-int",
    ];

    public static async Task<string> RunAsync(string name)
    {
        var holder = name switch
        {
            "svm-async-int" or "svm-async-int-direct" => Create(typeof(SvmAsyncHolder<int>)),
            "svm-async-double" => Create(typeof(SvmAsyncHolder<double>)),
            "nosvm-async-int" => Create(typeof(NoSvmAsyncHolder<int>)),
            "svm-sync-int" => Create(typeof(SvmSyncHolder<int>)),
            "nosvm-sync-int" => Create(typeof(NoSvmSyncHolder<int>)),
            _ => throw new ArgumentException($"Unknown case '{name}'.", nameof(name)),
        };

        var setter = holder.CreateSetter();
        if (name.EndsWith("-direct", StringComparison.Ordinal))
        {
            await setter("181");
        }
        else
        {
            var binder = Binder.Create<string?>(setter, static value => value?.ToString());
            await (Task)binder.DynamicInvoke("181")!;
        }

        return holder.ValueText;
    }

    // Blazor creates components from a Type, so AOT never sees a direct `new` of the holder.
    private static HolderBase Create(Type type) => (HolderBase)Activator.CreateInstance(type)!;
}
