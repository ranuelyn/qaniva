using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Qaniva.Clinical.Core.Model;

namespace Qaniva.Clinical.Core.Engine;

/// <summary>
/// Evaluates the case mini-expression language against a <see cref="PatientState"/>.
///
/// Grammar (recursive descent):
///   expr       := or
///   or         := and ( "||" and )*
///   and        := unary ( "&&" unary )*
///   unary      := "!" unary | comparison
///   comparison := primary ( ("=="|"!="|"&lt;"|"&lt;="|"&gt;"|"&gt;=") primary )?
///   primary    := number | string | "true" | "false"
///               | "flag" "(" string ")" | "disclosed" "(" string ")" | "actionCount" "(" string ")"
///               | accessor | "(" expr ")"
///   accessor   := IDENT ( "." IDENT )*
///
/// The language is read-only: no assignment, no side effects. This keeps case
/// logic declarative and the engine deterministic.
/// </summary>
public static class ExpressionEvaluator
{
    public static bool EvaluateBool(string expression, PatientState state)
    {
        var value = Evaluate(expression, state);
        if (value.Kind != ValKind.Bool)
        {
            throw new ExpressionException(
                $"Expression did not evaluate to a boolean: \"{expression}\"");
        }
        return value.BoolValue;
    }

    public static Val Evaluate(string expression, PatientState state)
    {
        var tokens = Tokenize(expression);
        var parser = new Parser(tokens, expression, state);
        var result = parser.ParseExpression();
        parser.ExpectEnd();
        return result;
    }

    // --- Tokenizer -------------------------------------------------------

    private enum TokKind { Op, Number, String, Ident, LParen, RParen, End }

    private readonly struct Token
    {
        public Token(TokKind kind, string text)
        {
            Kind = kind;
            Text = text;
        }

        public TokKind Kind { get; }
        public string Text { get; }
    }

    private static List<Token> Tokenize(string src)
    {
        var tokens = new List<Token>();
        int i = 0;
        while (i < src.Length)
        {
            char c = src[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (c == '(')
            {
                tokens.Add(new Token(TokKind.LParen, "("));
                i++;
                continue;
            }

            if (c == ')')
            {
                tokens.Add(new Token(TokKind.RParen, ")"));
                i++;
                continue;
            }

            if (c == ',')
            {
                tokens.Add(new Token(TokKind.Op, ","));
                i++;
                continue;
            }

            if (c == '.')
            {
                tokens.Add(new Token(TokKind.Op, "."));
                i++;
                continue;
            }

            if (c == '\'')
            {
                int start = ++i;
                var sb = new StringBuilder();
                while (i < src.Length && src[i] != '\'')
                {
                    sb.Append(src[i]);
                    i++;
                }
                if (i >= src.Length)
                {
                    throw new ExpressionException($"Unterminated string literal in \"{src}\"");
                }
                i++; // closing quote
                _ = start;
                tokens.Add(new Token(TokKind.String, sb.ToString()));
                continue;
            }

            if (c == '&' || c == '|')
            {
                if (i + 1 < src.Length && src[i + 1] == c)
                {
                    tokens.Add(new Token(TokKind.Op, c == '&' ? "&&" : "||"));
                    i += 2;
                    continue;
                }
                throw new ExpressionException($"Unexpected '{c}' in \"{src}\" (did you mean '{c}{c}'?)");
            }

            if (c == '!' || c == '=' || c == '<' || c == '>')
            {
                if (i + 1 < src.Length && src[i + 1] == '=')
                {
                    tokens.Add(new Token(TokKind.Op, $"{c}="));
                    i += 2;
                    continue;
                }
                if (c == '=')
                {
                    throw new ExpressionException($"'=' is not valid; use '==' in \"{src}\"");
                }
                tokens.Add(new Token(TokKind.Op, c.ToString()));
                i++;
                continue;
            }

            if (char.IsDigit(c) || (c == '-' && i + 1 < src.Length && char.IsDigit(src[i + 1])))
            {
                int start = i;
                if (src[i] == '-')
                {
                    i++;
                }
                while (i < src.Length && (char.IsDigit(src[i]) || src[i] == '.'))
                {
                    i++;
                }
                tokens.Add(new Token(TokKind.Number, src.Substring(start, i - start)));
                continue;
            }

            if (char.IsLetter(c) || c == '_')
            {
                int start = i;
                while (i < src.Length && (char.IsLetterOrDigit(src[i]) || src[i] == '_'))
                {
                    i++;
                }
                tokens.Add(new Token(TokKind.Ident, src.Substring(start, i - start)));
                continue;
            }

            throw new ExpressionException($"Unexpected character '{c}' in \"{src}\"");
        }

        tokens.Add(new Token(TokKind.End, ""));
        return tokens;
    }

    // --- Parser --------------------------------------------------------

    private sealed class Parser
    {
        private readonly List<Token> _tokens;
        private readonly string _src;
        private readonly PatientState _state;
        private int _pos;

        public Parser(List<Token> tokens, string src, PatientState state)
        {
            _tokens = tokens;
            _src = src;
            _state = state;
        }

        private Token Current => _tokens[_pos];

        private Token Advance() => _tokens[_pos++];

        private bool MatchOp(string op)
        {
            if (Current.Kind == TokKind.Op && Current.Text == op)
            {
                _pos++;
                return true;
            }
            return false;
        }

        public void ExpectEnd()
        {
            if (Current.Kind != TokKind.End)
            {
                throw new ExpressionException($"Unexpected trailing token '{Current.Text}' in \"{_src}\"");
            }
        }

        public Val ParseExpression() => ParseOr();

        private Val ParseOr()
        {
            var left = ParseAnd();
            while (MatchOp("||"))
            {
                var right = ParseAnd();
                left = Val.Bool(left.AsBool(_src) || right.AsBool(_src));
            }
            return left;
        }

        private Val ParseAnd()
        {
            var left = ParseUnary();
            while (MatchOp("&&"))
            {
                var right = ParseUnary();
                left = Val.Bool(left.AsBool(_src) && right.AsBool(_src));
            }
            return left;
        }

        private Val ParseUnary()
        {
            if (MatchOp("!"))
            {
                var operand = ParseUnary();
                return Val.Bool(!operand.AsBool(_src));
            }
            return ParseComparison();
        }

        private Val ParseComparison()
        {
            var left = ParsePrimary();
            foreach (var op in ComparisonOps)
            {
                if (Current.Kind == TokKind.Op && Current.Text == op)
                {
                    _pos++;
                    var right = ParsePrimary();
                    return Val.Bool(Compare(left, right, op));
                }
            }
            return left;
        }

        private static readonly string[] ComparisonOps = { "==", "!=", "<=", ">=", "<", ">" };

        private bool Compare(Val a, Val b, string op)
        {
            switch (op)
            {
                case "==":
                    return ValuesEqual(a, b);
                case "!=":
                    return !ValuesEqual(a, b);
                default:
                    double an = a.AsNumber(_src);
                    double bn = b.AsNumber(_src);
                    return op switch
                    {
                        "<" => an < bn,
                        "<=" => an <= bn,
                        ">" => an > bn,
                        ">=" => an >= bn,
                        _ => throw new ExpressionException($"Unknown operator '{op}'"),
                    };
            }
        }

        private static bool ValuesEqual(Val a, Val b)
        {
            if (a.Kind == ValKind.Number && b.Kind == ValKind.Number)
            {
                return Math.Abs(a.NumberValue - b.NumberValue) < 1e-9;
            }
            if (a.Kind == ValKind.Bool || b.Kind == ValKind.Bool)
            {
                return a.Kind == b.Kind && a.BoolValue == b.BoolValue;
            }
            return string.Equals(a.ToStringValue(), b.ToStringValue(), StringComparison.Ordinal);
        }

        private Val ParsePrimary()
        {
            var tok = Current;

            if (tok.Kind == TokKind.LParen)
            {
                Advance();
                var inner = ParseExpression();
                if (Current.Kind != TokKind.RParen)
                {
                    throw new ExpressionException($"Expected ')' in \"{_src}\"");
                }
                Advance();
                return inner;
            }

            if (tok.Kind == TokKind.Number)
            {
                Advance();
                return Val.Number(double.Parse(tok.Text, CultureInfo.InvariantCulture));
            }

            if (tok.Kind == TokKind.String)
            {
                Advance();
                return Val.String(tok.Text);
            }

            if (tok.Kind == TokKind.Ident)
            {
                return ParseIdentifierOrCall();
            }

            throw new ExpressionException($"Unexpected token '{tok.Text}' in \"{_src}\"");
        }

        private Val ParseIdentifierOrCall()
        {
            string name = Advance().Text;

            if (Current.Kind == TokKind.LParen)
            {
                Advance();
                if (Current.Kind != TokKind.String)
                {
                    throw new ExpressionException(
                        $"{name}(...) expects a single quoted-string argument in \"{_src}\"");
                }
                string arg = Advance().Text;
                if (Current.Kind != TokKind.RParen)
                {
                    throw new ExpressionException($"Expected ')' after {name}('{arg}' in \"{_src}\"");
                }
                Advance();

                return name switch
                {
                    "flag" => Val.Bool(_state.HasFlag(arg)),
                    "disclosed" => Val.Bool(_state.DisclosedFacts.Contains(arg)),
                    "actionCount" => Val.Number(_state.ActionCount(arg)),
                    _ => throw new ExpressionException($"Unknown function '{name}' in \"{_src}\""),
                };
            }

            switch (name)
            {
                case "true":
                    return Val.Bool(true);
                case "false":
                    return Val.Bool(false);
                case "simTimeSec":
                    return Val.Number(_state.SimTimeSec);
                case "painScore":
                    return Val.Number(_state.PainScore);
                case "rhythm":
                    return Val.String(_state.Rhythm);
                case "airway":
                    return Val.String(_state.Airway);
                case "breathing":
                    return Val.String(_state.Breathing);
                case "circulation":
                    return Val.String(_state.Circulation);
                case "neuro":
                    return Val.String(_state.Neuro);
                case "vitals":
                    if (Current.Kind == TokKind.Op && Current.Text == ".")
                    {
                        Advance();
                        if (Current.Kind != TokKind.Ident)
                        {
                            throw new ExpressionException($"Expected a vital name after 'vitals.' in \"{_src}\"");
                        }
                        string vital = Advance().Text;
                        try
                        {
                            return Val.Number(_state.Vitals.Get(vital));
                        }
                        catch (ArgumentOutOfRangeException ex)
                        {
                            throw new ExpressionException($"Unknown vital 'vitals.{vital}' in \"{_src}\"", ex);
                        }
                    }
                    throw new ExpressionException($"'vitals' must be used as 'vitals.<name>' in \"{_src}\"");
                default:
                    throw new ExpressionException($"Unknown accessor '{name}' in \"{_src}\"");
            }
        }
    }
}

public enum ValKind
{
    Number,
    String,
    Bool,
}

public readonly struct Val
{
    private Val(ValKind kind, double number, string? str, bool boolean)
    {
        Kind = kind;
        NumberValue = number;
        StringValue = str;
        BoolValue = boolean;
    }

    public ValKind Kind { get; }
    public double NumberValue { get; }
    public string? StringValue { get; }
    public bool BoolValue { get; }

    public static Val Number(double value) => new(ValKind.Number, value, null, false);

    public static Val String(string value) => new(ValKind.String, 0, value, false);

    public static Val Bool(bool value) => new(ValKind.Bool, 0, null, value);

    public bool AsBool(string src) => Kind == ValKind.Bool
        ? BoolValue
        : throw new ExpressionException($"Expected a boolean value in \"{src}\"");

    public double AsNumber(string src) => Kind == ValKind.Number
        ? NumberValue
        : throw new ExpressionException($"Expected a numeric value in \"{src}\"");

    public string ToStringValue() => Kind switch
    {
        ValKind.Number => NumberValue.ToString(CultureInfo.InvariantCulture),
        ValKind.Bool => BoolValue ? "true" : "false",
        _ => StringValue ?? "",
    };
}

public sealed class ExpressionException : Exception
{
    public ExpressionException(string message)
        : base(message)
    {
    }

    public ExpressionException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
