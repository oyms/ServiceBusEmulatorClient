using Skaar.ServiceBusEmulatorClient.Serializing;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.Json.Serialization;

namespace Skaar.ServiceBusEmulatorClient.Model;

[JsonConverter(typeof(ParsableJsonConverter<MessageId>))]
public readonly struct MessageId : IParsable<MessageId>, IEquatable<MessageId>, IComparable<MessageId>,
    IComparisonOperators<MessageId, MessageId, bool>
{
    #region Parsing and rendering

    private readonly string? _value;

    private MessageId(string? value)
    {
        _value = value;
    }

    public static MessageId Parse(string s, IFormatProvider? provider = null)
    {
        if (!TryParse(s, provider, out var result) && !result.IsValid)
        {
            throw new FormatException("String is not a valid MessageId.");
        }

        return result;
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out MessageId result)
    {
        result = new MessageId(s);
        return result.IsValid;
    }

    public override string ToString()
    {
        return IsValid ? _value! : string.Empty;
    }

    public bool IsValid => !string.IsNullOrEmpty(_value);

    #endregion

    #region Equality and comparison

    public bool Equals(MessageId other)
    {
        return _value == other._value;
    }

    public override bool Equals(object? obj)
    {
        return obj is MessageId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _value != null ? _value!.GetHashCode(StringComparison.InvariantCulture) : 0;
    }

    public static bool operator ==(MessageId left, MessageId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MessageId left, MessageId right)
    {
        return !(left == right);
    }

    public int CompareTo(MessageId other)
    {
        return string.Compare(_value, other._value, StringComparison.Ordinal);
    }

    public static bool operator <(MessageId left, MessageId right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(MessageId left, MessageId right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >(MessageId left, MessageId right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator >=(MessageId left, MessageId right)
    {
        return left.CompareTo(right) >= 0;
    }

    #endregion
}