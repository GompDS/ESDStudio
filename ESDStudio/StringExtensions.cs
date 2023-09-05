using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ESDStudio;

public static class StringExtensions
{
    public static List<int> AllIndexesOf(this string str, string value, int startIndex = 0) {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("the string to find may not be empty", nameof(value));
        List<int> indexes = new List<int>();
        for (int index = startIndex;; index += value.Length) {
            index = str.IndexOf(value, index, StringComparison.Ordinal);
            if (index == -1)
                return indexes;
            indexes.Add(index);
        }
    }
    
    public static string ReplaceMatches(this string input, string searchTerm, string replacement, bool useRegex, bool matchWholeWord)
    {
        if (!useRegex && !matchWholeWord)
        {
            input = input.Replace(searchTerm, replacement);
        }
        else
        {
            string pattern = useRegex ? searchTerm : Regex.Escape(searchTerm);
            if (matchWholeWord)
            {
                pattern = $"\\b{pattern}\\b";
            }

            Regex regex = new Regex(pattern);
            input = regex.Replace(input, replacement);
            //input = Regex.Replace(input, pattern);
            
            /*MatchCollection matches = Regex.Matches(input, pattern);
            int startIndex = 0;
            foreach (Match match in matches)
            {
                input = input.Remove(match.Index + startIndex, match.Length);
                input = input.Insert(match.Index + startIndex, replacement);
                startIndex += match.Index + replacement.Length;
            }*/
            
            /*Match match = Regex.Match(input, pattern);
            int startIndex = 0;
            while (match.Success)
            {
                input = input.Remove(match.Index + startIndex, match.Length);
                input = input.Insert(match.Index + startIndex, replacement);
                startIndex += match.Index + replacement.Length;
                match = Regex.Match(input[startIndex..], pattern);
            }*/
        }

        return input;
    }
}