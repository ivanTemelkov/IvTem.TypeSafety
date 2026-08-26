using System;
using System.Diagnostics.CodeAnalysis;
using IvTem.TypeSafety;

namespace IvTem.TypeSafety.Sample;

internal static class Program
{
    private static void Main()
    {
        var result = new OperationResult<string>("completed");
        var envelope = MessageEnvelope.CreateValue("factory-completed");
        var errorEnvelope = MessageEnvelope.CreateError<string>(new InvalidOperationException("factory-failed"));
        var payload = new ExactPayload<object>(new object());
        var repository = new CustomerRepository();
        var parser = new Parser();
        Factory<object> factory = static () => new object();

        repository.Save(new Customer("A100"));
        Parser.Parse<string>("sample");
        parser.ConvertLocal<string>("local");

        Console.WriteLine(result.Value);
        Console.WriteLine(envelope.Payload);
        Console.WriteLine(errorEnvelope.HasValue());
        Console.WriteLine(payload.Value.GetType().Name);
        Console.WriteLine(factory().GetType().Name);
    }
}

internal sealed class OperationResult<[DisallowTypes(typeof(Exception))] T>
{
    public OperationResult(T value)
    {
        Value = value;
    }

    public T Value { get; }
}

internal static class MessageEnvelope
{
    public static MessageEnvelope<T> CreateError<[DisallowTypes(typeof(Exception))] T>([NotNull] Exception error)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(error);

        return MessageEnvelope<T>.CreateError(error);
    }

    public static MessageEnvelope<T> CreateValue<[DisallowTypes(typeof(Exception))] T>([NotNull] T payload)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(payload);

        return new(payload);
    }
}

internal sealed class MessageEnvelope<T>
    where T : notnull
{
    private Exception? Error { get; init; }

    private MessageEnvelope()
    {
    }

    internal MessageEnvelope([NotNull] T payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        Payload = payload;
    }

    public T? Payload { get; set; }

    internal static MessageEnvelope<T> CreateError([NotNull] Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new()
        {
            Error = error
        };
    }

    public bool HasValue()
        => Error is null;
}

internal readonly struct Customer
{
    public Customer(string id)
    {
        Id = id;
    }

    public string Id { get; }
}

internal sealed class ExactPayload<[DisallowExactTypes(typeof(string))] T>
{
    public ExactPayload(T value)
    {
        Value = value;
    }

    public T Value { get; }
}

internal interface IRepository
{
    void Save<[DisallowTypes(typeof(Exception))] T>(T item);
}

internal sealed class CustomerRepository : IRepository
{
    public void Save<T>(T item)
    {
        _ = item;
    }
}

internal delegate T Factory<[DisallowExactTypes(typeof(string))] T>();

internal sealed class Parser
{
    public static T Parse<[DisallowTypes(typeof(Exception))] T>(string value)
    {
        return (T)Convert.ChangeType(value, typeof(T));
    }

    public T ConvertLocal<T>(T value)
    {
        static TLocal Identity<[DisallowExactTypes(typeof(Exception))] TLocal>(TLocal local)
        {
            return local;
        }

        return Identity(value);
    }
}
