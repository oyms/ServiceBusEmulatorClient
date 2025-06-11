using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.Json.Serialization;
using Skaar.ServiceBusEmulatorClient.Serializing;

namespace Skaar.ServiceBusEmulatorClient.Model;

[JsonConverter(typeof(ParsableJsonConverter<SubscriptionName>))]
public readonly struct SubscriptionName : IParsable<SubscriptionName>, IEquatable<SubscriptionName>,
    IComparable<SubscriptionName>, IComparisonOperators<SubscriptionName, SubscriptionName, bool>
{
    #region Parsing and rendering

    private readonly string? _value;

    private SubscriptionName(string? value)
    {
        _value = value;
    }

    public static SubscriptionName Parse(string s, IFormatProvider? provider = null)
    {
        if (!TryParse(s, provider, out var result) && !result.IsValid)
        {
            throw new FormatException("String is not a valid SubscriptionName.");
        }

        return result;
    }

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out SubscriptionName result)
    {
        result = new SubscriptionName(s);
        return result.IsValid;
    }

    public override string ToString()
    {
        return IsValid ? _value! : string.Empty;
    }

    public bool IsValid => !string.IsNullOrEmpty(_value);

    #endregion

    #region Equality and comparison

    public bool Equals(SubscriptionName other)
    {
        return _value == other._value;
    }

    public override bool Equals(object? obj)
    {
        return obj is SubscriptionName other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _value != null ? _value!.GetHashCode(StringComparison.InvariantCulture) : 0;
    }

    public static bool operator ==(SubscriptionName left, SubscriptionName right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SubscriptionName left, SubscriptionName right)
    {
        return !(left == right);
    }

    public int CompareTo(SubscriptionName other)
    {
        return string.Compare(_value, other._value, StringComparison.Ordinal);
    }

    public static bool operator <(SubscriptionName left, SubscriptionName right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(SubscriptionName left, SubscriptionName right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >(SubscriptionName left, SubscriptionName right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator >=(SubscriptionName left, SubscriptionName right)
    {
        return left.CompareTo(right) >= 0;
    }

    #endregion
}