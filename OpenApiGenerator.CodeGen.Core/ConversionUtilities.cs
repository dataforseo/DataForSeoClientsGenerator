using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CodeGenerator.Core;


    public class ConversionUtilities
    {
        private static readonly Regex HtmlAnchorRegex = new(
            @"<a\s+[^>]*href\s*=\s*[""'](?<href>[^""']+)[""'][^>]*>(?<text>.*?)</a>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex HtmlCodeRegex = new(
            @"<code\b[^>]*>(?<text>.*?)</code>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex HtmlStrongRegex = new(
            @"<(strong|b)\b[^>]*>(?<text>.*?)</\1>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex HtmlEmphasisRegex = new(
            @"<(em|i)\b[^>]*>(?<text>.*?)</\1>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex HtmlBreakRegex = new(
            @"<br\s*/?>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex HtmlParagraphRegex = new(
            @"</?p\b[^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex HtmlTagRegex = new(
            @"<[^>]+>",
            RegexOptions.Compiled);

        private static readonly Regex MultiNewlineRegex = new(
            @"\n{2,}",
            RegexOptions.Compiled);

        public static string ConvertToLowerCamelCase(string input, bool firstCharacterMustBeAlpha)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            input = ConvertDashesToCamelCase(
                (input[0].ToString().ToLowerInvariant() + (input.Length > 1 ? input.Substring(1) : ""))
                .Replace(" ", "_")
                .Replace("/", "_"));

            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            if (firstCharacterMustBeAlpha && char.IsNumber(input[0]))
            {
                return "_" + input;
            }

            return input;
        }
        
        public static string ConvertToUpperCamelCase(string input, bool firstCharacterMustBeAlpha)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            input = ConvertDashesToCamelCase(Capitalize(input)
                .Replace(" ", "_")
                .Replace("/", "_"));

            if (firstCharacterMustBeAlpha && char.IsNumber(input[0]))
            {
                return "_" + input;
            }

            return input;
        }

        [MethodImpl((MethodImplOptions) 256)]
        private static string Capitalize(string input)
        {
            if (char.IsUpper(input[0]))
            {
                return input;
            }
            if (input.Length == 1)
            {
                return char.ToUpperInvariant(input[0]).ToString();
            }
            return char.ToUpperInvariant(input[0]) + input.Substring(1);
        }

        public static string ConvertToStringLiteral(string input)
        {
            var literal = new StringBuilder(input.Length);
            foreach (var c in input)
            {
                switch (c)
                {
                    case '\'':
                        literal.Append(@"\'");
                        break;
                    case '\"':
                        literal.Append("\\\"");
                        break;
                    case '\\':
                        literal.Append(@"\\");
                        break;
                    case '\0':
                        literal.Append(@"\0");
                        break;
                    case '\a':
                        literal.Append(@"\a");
                        break;
                    case '\b':
                        literal.Append(@"\b");
                        break;
                    case '\f':
                        literal.Append(@"\f");
                        break;
                    case '\n':
                        literal.Append(@"\n");
                        break;
                    case '\r':
                        literal.Append(@"\r");
                        break;
                    case '\t':
                        literal.Append(@"\t");
                        break;
                    case '\v':
                        literal.Append(@"\v");
                        break;
                    default:
                        // ASCII printable character
                        if (c >= 0x20 && c <= 0x7e)
                        {
                            literal.Append(c);
                            // As UTF16 escaped character
                        }
                        else
                        {
                            literal.Append(@"\u");
                            literal.Append(((int) c).ToString("x4", CultureInfo.InvariantCulture));
                        }

                        break;
                }
            }

            return literal.ToString();
        }

        public static string ConvertToCamelCase(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            return ConvertDashesToCamelCase(input.Replace(" ", "_").Replace("/", "_"));
        }


        private static readonly char[] _whiteSpaceChars = { '\n', '\r', '\t', ' ' };

        public static string TrimWhiteSpaces(string? text)
        {
            return text?.Trim(_whiteSpaceChars) ?? string.Empty;
        }

        private static readonly char[] _lineBreakTrimChars = { '\n', '\t', ' ' };

        public static string RemoveLineBreaks(string? text)
        {
            return text?.Replace("\r", "")
                .Replace("\n", " \n")
                .Replace("\n ", "\n")
                .Replace("  \n", " \n")
                .Replace("\n", "")
                .Trim(_lineBreakTrimChars) ?? string.Empty;
        }

        public static string Singularize(string word)
        {
            if (word == "people")
            {
                return "Person";
            }

            return word.EndsWith('s') ? word.Substring(0, word.Length - 1) : word;
        }

        public static string Tab(string input, int tabCount)
        {
            if (input is null)
            {
                return "";
            }
            var stringWriter = new StringWriter(new StringBuilder(input.Length), CultureInfo.CurrentCulture);
            Tab(input, tabCount, stringWriter);
            return stringWriter.ToString();
        }

        public static void Tab(string input, int tabCount, TextWriter writer)
        {
            var tabString = CreateWhitespaceString(tabCount);
            AddPrefixToBeginningOfNonEmptyLines(input, tabString, writer);
        }

        private static void AddPrefixToBeginningOfNonEmptyLines(string input, string tabString, TextWriter writer)
        {
            if (tabString.Length == 0)
            {
                return;
            }

            for (var i = 0; i < input.Length; i++)
            {
                var c = input[i];
                writer.Write(c);
                if (c == '\n')
                {
                    // only write if not entirely empty line
                    var foundNonEmptyBeforeNewLine = false;
                    for (var j = i + 1; j < input.Length; ++j)
                    {
                        var c2 = input[j];
                        if (c2 == '\n')
                        {
                            break;
                        }

                        if (!char.IsWhiteSpace(c2))
                        {
                            foundNonEmptyBeforeNewLine = true;
                            break;
                        }
                    }

                    if (foundNonEmptyBeforeNewLine)
                    {
                        writer.Write(tabString);
                    }
                }
            }
        }

        /// <summary>
        /// Converts OpenAPI/HTML descriptions into C# XML-doc markup, then formats continuation lines.
        /// </summary>
        public static string ConvertCSharpDocs(string input, int tabCount)
        {
            input = HtmlToCSharpDocs(input);
            input = input?
                        .Replace("\r", string.Empty)
                        .Replace("\n", "\n" + string.Join("", Enumerable.Repeat("    ", tabCount)) + "/// ")
                    ?? string.Empty;

            return Regex.Replace(input, @"^( *)/// ", m => m.Groups[1] + "/// <br/>", RegexOptions.Multiline);
        }

        /// <summary>
        /// Keeps HTML suitable for Javadoc (which natively supports a subset of HTML).
        /// </summary>
        public static string ConvertJavaDocs(string input, int tabCount)
        {
            input = HtmlToJavaDocs(input);
            input = input?
                        .Replace("\r", string.Empty)
                        .Replace("\n", "\n" + string.Join("", Enumerable.Repeat("    ", tabCount)) + "* ")
                    ?? string.Empty;

            return input;
        }

        /// <summary>
        /// Converts HTML descriptions into a single-line markdown-ish string for Python Field(description=...).
        /// </summary>
        public static string ConvertPythonDocs(string input)
        {
            var text = HtmlToMarkdownDocs(input);
            return text.Replace("\r", string.Empty).Replace("\n", ". ");
        }

        /// <summary>
        /// Converts HTML descriptions into markdown suitable for JSDoc / TSDoc comments.
        /// </summary>
        public static string ConvertTypeScriptDocs(string input)
        {
            // Avoid prematurely closing the block comment when markdown contains "*/"
            // (e.g. bold wrapping a wildcard path like **/*).
            return HtmlToMarkdownDocs(input)
                .Replace("\r", string.Empty)
                .Replace("\n", " ")
                .Replace("*/", "* /");
        }

        public static string HtmlToCSharpDocs(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input ?? string.Empty;

            var text = WebUtility.HtmlDecode(input);
            var preserved = new List<string>();

            string Preserve(string xmlSnippet)
            {
                var index = preserved.Count;
                preserved.Add(xmlSnippet);
                return $"@@DOC{index}@@";
            }

            text = HtmlAnchorRegex.Replace(text, m =>
            {
                var label = EscapeXmlText(FlattenInline(m.Groups["text"].Value));
                var href = EscapeXmlAttribute(m.Groups["href"].Value);
                return Preserve($"<see href=\"{href}\">{label}</see>");
            });

            text = HtmlCodeRegex.Replace(text, m =>
            {
                var code = EscapeXmlText(FlattenInline(m.Groups["text"].Value));
                return Preserve($"<c>{code}</c>");
            });

            text = HtmlBreakRegex.Replace(text, "\n");
            text = HtmlParagraphRegex.Replace(text, "\n");
            text = HtmlTagRegex.Replace(text, string.Empty);
            text = MultiNewlineRegex.Replace(text, "\n").Trim();
            text = EscapeXmlText(text);

            return Regex.Replace(text, "@@DOC(\\d+)@@", m => preserved[int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)]);
        }

        public static string HtmlToJavaDocs(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input ?? string.Empty;

            var text = WebUtility.HtmlDecode(input);
            // Javadoc accepts HTML; normalize voids and avoid prematurely closing the comment.
            text = HtmlBreakRegex.Replace(text, "<br>");
            text = text.Replace("*/", "* /");
            return text.Trim();
        }

        public static string HtmlToMarkdownDocs(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input ?? string.Empty;

            var text = WebUtility.HtmlDecode(input);

            text = HtmlAnchorRegex.Replace(text, m =>
                $"[{FlattenInline(m.Groups["text"].Value)}]({m.Groups["href"].Value})");

            text = HtmlCodeRegex.Replace(text, m => $"`{FlattenInline(m.Groups["text"].Value)}`");
            text = HtmlStrongRegex.Replace(text, m => $"**{FlattenInline(m.Groups["text"].Value)}**");
            text = HtmlEmphasisRegex.Replace(text, m => $"*{FlattenInline(m.Groups["text"].Value)}*");
            text = HtmlBreakRegex.Replace(text, "\n");
            text = HtmlParagraphRegex.Replace(text, "\n");
            text = HtmlTagRegex.Replace(text, string.Empty);
            text = MultiNewlineRegex.Replace(text, "\n").Trim();
            return text;
        }

        private static string FlattenInline(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            text = HtmlTagRegex.Replace(text, string.Empty);
            return text.Replace("\r", string.Empty).Replace("\n", " ").Trim();
        }

        private static string EscapeXmlText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return new XText(text).ToString();
        }

        private static string EscapeXmlAttribute(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return text
                .Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }

        private static string CreateWhitespaceString(int wsCount)
        {
            return wsCount switch // 0,1,2 -to improve performance
            {
                0 => "", 
                1 => " ",
                2 => "  ",
                > 2 => new string(' ', wsCount)
            };
        }

        private static string ConvertDashesToCamelCase(string input)
        {
            if (!input.Contains('-'))
            {
                // no conversion necessary
                return input;
            }

            // we are removing at least one character
            var sb = new StringBuilder(input.Length - 1);
            var caseFlag = false;
            foreach (var c in input)
            {
                if (c == '-')
                {
                    caseFlag = true;
                }
                else if (caseFlag)
                {
                    sb.Append(char.ToUpperInvariant(c));
                    caseFlag = false;
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }