using System;
using System.Collections.Generic;
using System.Text;

namespace MasterMemory.Generator.Internal;

public static class StringBuilderExtensions
{
    public static string ToStringAndClear(this StringBuilder sb)
    {
        string result = sb.ToString();
        sb.Clear();
        return result;
    }

    public static StringBuilder AppendDecapitalized(this StringBuilder sb, string? value)
    {
        if (value?.Length is null or 0)
            return sb;

        return sb.Append(char.ToLower(value[0])).Append(value, 1, value.Length - 1);
    }

    public static StringBuilder AppendCapitalized(this StringBuilder sb, string? value)
    {
        if (value?.Length is null or 0)
            return sb;

        return sb.Append(char.ToUpper(value[0])).Append(value, 1, value.Length - 1);
    }

    public static StringBuilder AppendBuilder(this StringBuilder sb, StringBuilder value)
    {
        sb.EnsureCapacity(sb.Length + value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            sb.Append(value[i]);
        }

        return sb;
    }

    public static StringBuilder AppendJoin<T>(this StringBuilder sb, string separator, IEnumerable<T> values,
        Action<StringBuilder, T> append)
    {
        using var enumerator = values.GetEnumerator();
        if (enumerator.MoveNext())
        {
            append(sb, enumerator.Current);
            while (enumerator.MoveNext())
            {
                sb.Append(separator);
                append(sb, enumerator.Current);
            }
        }

        return sb;
    }

    public static StringBuilder AppendJoin<T>(this StringBuilder sb, string separator, IEnumerable<T> values,
        Action<T> append)
    {
        using var enumerator = values.GetEnumerator();
        if (enumerator.MoveNext())
        {
            append(enumerator.Current);
            while (enumerator.MoveNext())
            {
                sb.Append(separator);
                append(enumerator.Current);
            }
        }

        return sb;
    }

    public static StringBuilder Append(this StringBuilder sb, bool condition, string? value)
    {
        if (condition)
        {
            sb.Append(value);
        }

        return sb;
    }

    public static StringBuilder AppendIntend(this StringBuilder sb, int intendLevel)
    {
        const int intendLength = 4;
        sb.EnsureCapacity(sb.Capacity + intendLevel * intendLength);
        for (int i = 0; i < intendLevel; i++)
        {
            sb.Append(' ', intendLength);
        }

        return sb;
    }

    public static StringBuilder RemoveFrom(this StringBuilder sb, int index)
    {
        return sb.Remove(index, sb.Length - index);
    }

    public static int IndexOf(this StringBuilder sb, char c)
    {
        for (int i = 0; i < sb.Length; i++)
        {
            if (c == sb[i])
            {
                return i;
            }
        }

        return -1;
    }

    public static StringBuilderScope Scope(this StringBuilder sb, bool condition, string open, string close)
    {
        if (condition)
        {
            return Scope(sb, open, close);
        }

        return StringBuilderScope.Empty;
    }

    public static StringBuilderScope Scope(this StringBuilder sb, string open, string close)
    {
        return new StringBuilderScope(sb, open, close);
    }

    public static StringBuilderScope Scope(this StringBuilder sb, bool condition, char open)
    {
        if (condition)
        {
            return Scope(sb, open);
        }

        return StringBuilderScope.Empty;
    }

    public static StringBuilderScope BracketScope(this StringBuilder sb, bool condition = true)
    {
        return Scope(sb, condition, '{');
    }

    public static StringBuilderScope Scope(this StringBuilder sb, char open)
    {
        string openStr = open switch
        {
            '(' => "(",
            '[' => "[",
            '{' => "{",
            '<' => "<",
            _ => throw new ArgumentException("Invalid open character")
        };

        string closeStr = open switch
        {
            '(' => ")",
            '[' => "]",
            '{' => "}",
            '<' => ">",
            _ => throw new ArgumentException("Invalid open character")
        };

        return new StringBuilderScope(sb, openStr, closeStr);
    }

    public static StringBuilderScope NamespaceScope(this StringBuilder sb, string? @namespace)
    {
        if (string.IsNullOrWhiteSpace(@namespace))
        {
            return StringBuilderScope.Empty;
        }

        sb.Append("namespace ").Append(@namespace).AppendLine();

        return sb.BracketScope();
    }

    public static StringBuilderScope LockScope(this StringBuilder sb, string? @lock)
    {
        return LockScope(sb, !string.IsNullOrWhiteSpace(@lock), @lock!);
    }

    public static StringBuilderScope LockScope(this StringBuilder sb, bool condition, string @lock)
    {
        if (!condition)
        {
            return StringBuilderScope.Empty;
        }

        sb.Append("lock (").Append(@lock).Append(')');

        return sb.BracketScope();
    }
}

public readonly ref struct StringBuilderScope
{
    private readonly StringBuilder _sb;
    private readonly string _close;

    public StringBuilderScope(StringBuilder sb, string open, string close)
    {
        _sb = sb;
        _close = close;
        if (open[0] == '#')
        {
            sb.AppendLine();
            _sb.AppendLine(open);
        }
        else
        {
            _sb.Append(open);
        }
    }

    public static StringBuilderScope Empty => default;

    public void Dispose()
    {
        if (_sb == null)
            return;

        if (_close[0] == '#')
        {
            _sb.AppendLine();
            _sb.AppendLine(_close);
        }
        else
        {
            _sb.Append(_close);
        }
    }
}