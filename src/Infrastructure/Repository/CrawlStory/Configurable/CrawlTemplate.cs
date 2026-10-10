using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SurveillanceCameras.Application.Common.Exceptions;

namespace SurveillanceCameras.Infrastructure.Repository.CrawlStory.Configurable;

/// <summary>
/// Tính template trong cấu hình crawl: mỗi đoạn <c>{...}</c> là một biểu thức gồm biến, chuỗi <c>'...'</c>, số,
/// phép nối <c>+</c> và các hàm dựng sẵn (xem <see cref="Functions"/>). Vd. <c>{sha256(rotate(signKey + offset, 62))}</c>.
/// </summary>
public static class CrawlTemplate
{
    private static readonly Dictionary<string, Func<string[], string>> Functions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sha256"] = args => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Arg(args, 0, 1)))),
        ["md5"] = args => Convert.ToHexStringLower(MD5.HashData(Encoding.UTF8.GetBytes(Arg(args, 0, 1)))),
        ["rotate"] = args => Rotate(Arg(args, 0, 2), Arg(args, 1, 2)),
        ["lower"] = args => Arg(args, 0, 1).ToLowerInvariant(),
        ["upper"] = args => Arg(args, 0, 1).ToUpperInvariant(),
        ["urlencode"] = args => Uri.EscapeDataString(Arg(args, 0, 1)),
    };

    public static string Render(string template, IReadOnlyDictionary<string, string> vars)
    {
        var result = new StringBuilder();
        var i = 0;

        while (i < template.Length)
        {
            var open = template.IndexOf('{', i);
            if (open < 0)
            {
                result.Append(template, i, template.Length - i);
                break;
            }

            result.Append(template, i, open - i);

            var close = FindClose(template, open);
            result.Append(Evaluate(template[(open + 1)..close], vars));
            i = close + 1;
        }

        return result.ToString();
    }

    public static string Evaluate(string expression, IReadOnlyDictionary<string, string> vars)
    {
        var parser = new Parser(expression, vars);
        var value = parser.ParseExpression();
        parser.ExpectEnd();
        return value;
    }

    // Tìm '}' đóng, bỏ qua '}' nằm trong chuỗi '...'.
    private static int FindClose(string template, int open)
    {
        var inString = false;
        for (var i = open + 1; i < template.Length; i++)
        {
            var c = template[i];
            if (inString && c == '\\') i++;
            else if (c == '\'') inString = !inString;
            else if (c == '}' && !inString) return i;
        }

        throw new CrawlException($"Template thiếu dấu '}}': {template}");
    }

    private static string Arg(string[] args, int index, int expected)
        => args.Length == expected
            ? args[index]
            : throw new CrawlException($"Hàm cần {expected} tham số nhưng nhận {args.Length}.");

    private static string Rotate(string text, string countText)
    {
        if (!int.TryParse(countText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count))
            throw new CrawlException($"rotate(): '{countText}' không phải số.");

        if (text.Length == 0) return text;

        // Giống JS text.substring(n) + text.substring(0, n): n vượt độ dài thì giữ nguyên chuỗi.
        count = Math.Clamp(count, 0, text.Length);
        return text[count..] + text[..count];
    }

    private sealed class Parser(string text, IReadOnlyDictionary<string, string> vars)
    {
        private int _pos;

        public string ParseExpression()
        {
            var value = new StringBuilder(ParseTerm());
            while (TryConsume('+')) value.Append(ParseTerm());
            return value.ToString();
        }

        public void ExpectEnd()
        {
            SkipSpaces();
            if (_pos < text.Length) throw Error($"ký tự thừa '{text[_pos]}'");
        }

        private string ParseTerm()
        {
            SkipSpaces();
            if (_pos >= text.Length) throw Error("thiếu giá trị");

            var c = text[_pos];

            if (c == '\'') return ParseString();

            if (char.IsDigit(c))
            {
                var start = _pos;
                while (_pos < text.Length && char.IsDigit(text[_pos])) _pos++;
                return text[start.._pos];
            }

            if (char.IsLetter(c) || c == '_')
            {
                var start = _pos;
                while (_pos < text.Length && (char.IsLetterOrDigit(text[_pos]) || text[_pos] == '_')) _pos++;
                var name = text[start.._pos];

                if (!TryConsume('('))
                {
                    return vars.TryGetValue(name, out var value)
                        ? value
                        : throw Error($"biến '{name}' chưa có giá trị");
                }

                if (!Functions.TryGetValue(name, out var function)) throw Error($"không có hàm '{name}'");

                var args = new List<string>();
                if (!TryConsume(')'))
                {
                    do args.Add(ParseExpression()); while (TryConsume(','));
                    if (!TryConsume(')')) throw Error($"thiếu ')' sau tham số của {name}()");
                }

                return function(args.ToArray());
            }

            throw Error($"ký tự không hợp lệ '{c}'");
        }

        private string ParseString()
        {
            var result = new StringBuilder();
            _pos++; // bỏ dấu ' mở

            while (_pos < text.Length)
            {
                var c = text[_pos++];
                if (c == '\'') return result.ToString();
                if (c == '\\' && _pos < text.Length) c = text[_pos++];
                result.Append(c);
            }

            throw Error("chuỗi thiếu dấu ' đóng");
        }

        private bool TryConsume(char c)
        {
            SkipSpaces();
            if (_pos < text.Length && text[_pos] == c)
            {
                _pos++;
                return true;
            }

            return false;
        }

        private void SkipSpaces()
        {
            while (_pos < text.Length && char.IsWhiteSpace(text[_pos])) _pos++;
        }

        private CrawlException Error(string message) => new($"Biểu thức '{{{text}}}' lỗi: {message}.");
    }
}
