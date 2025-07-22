using System.Text;
using System.Text.RegularExpressions;

namespace OpenApiGenerator.Utils.Extensions;

public static class StringExtension
{
    static readonly Regex WordRegex = new Regex(@"[A-Z]?[a-z]+|[A-Z]+(?![a-z])|\d+", RegexOptions.Compiled);
 
    public static string ToScreamingCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new StringBuilder();
        bool wasPreviousUnderscore = false;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (char.IsUpper(c) && i > 0 && !wasPreviousUnderscore)
            {
                result.Append('_');
            }

            if (char.IsWhiteSpace(c) || c == '-' || c == '_')
            {
                if (!wasPreviousUnderscore)
                {
                    result.Append('_');
                    wasPreviousUnderscore = true;
                }
            }
            else
            {
                result.Append(char.ToUpper(c));
                wasPreviousUnderscore = false;
            }
        }

        if (result.Length > 0 && result[^1] == '_')
        {
            result.Length--;
        }

        return result.ToString();
    }


    
    public static string ToPascalCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        string temp = input.Replace('_', ' ').Replace('-', ' ');

        var matches = WordRegex.Matches(temp);

        string result = "";
        foreach (Match match in matches)
        {
            string word = match.Value;
            if (word.Length > 0)
            {
                result += char.ToUpper(word[0]) + word.Substring(1).ToLower();
            }
        }

        return result;
    }
    
    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        string temp = input.Replace('-', ' ').Replace('_', ' ');
        var matches = WordRegex.Matches(temp);

        List<string> words = new List<string>();
        foreach (Match match in matches)
        {
            string word = match.Value;
            if (!string.IsNullOrEmpty(word))
            {
                words.Add(word.ToLower());
            }
        }

        string result = string.Join("_", words);
        return result;
    }
    
    public static string ToCamelCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        string temp = input.Replace('_', ' ').Replace('-', ' ');
        var matches = WordRegex.Matches(temp);

        string result = "";
        int index = 0;
        foreach (Match match in matches)
        {
            string word = match.Value;
            if (index == 0)
            {
                result += word.ToLower();
            }
            else
            {
                result += char.ToUpper(word[0]) + word.Substring(1).ToLower();
            }
            index++;
        }

        return result;
    }

    
    public static bool IsSnakeCase(this string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        return text.Split('_').All(x => !string.IsNullOrEmpty(x)  && char.IsLower(x[1]));
    }
}