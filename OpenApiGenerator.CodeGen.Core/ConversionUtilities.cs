using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CodeGenerator.Core;


    public class ConversionUtilities
    {
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

        public static string ConvertCSharpDocs(string input, int tabCount)
        {
            input = input?
                        .Replace("\r", string.Empty)
                        .Replace("\n", "\n" + string.Join("", Enumerable.Repeat("    ", tabCount)) + "/// ")
                    ?? string.Empty;

            // TODO: Support more markdown features here
            var xml = new XText(input).ToString();
            return Regex.Replace(xml, @"^( *)/// ", m => m.Groups[1] + "/// <br/>", RegexOptions.Multiline);
        }

        public static string ConvertJavaDocs(string input, int tabCount)
        {
            input = input?
                        .Replace("\r", string.Empty)
                        .Replace("\n", "\n" + string.Join("", Enumerable.Repeat("    ", tabCount)) + "* ")
                    ?? string.Empty;

            return input;
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