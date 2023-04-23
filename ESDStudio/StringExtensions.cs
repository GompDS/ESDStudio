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
    
    public static string ReplaceWordMatches(this string input, string searchWord, string replacement)
    {
        string pattern = Regex.Escape(searchWord);
        Match match = Regex.Match(input, $"\\b{pattern}\\b");
        int startIndex = 0;
        while (match.Success)
        {
            input = input.Remove(match.Index + startIndex, match.Length);
            input = input.Insert(match.Index + startIndex, replacement);
            startIndex += match.Index + replacement.Length;
            match = Regex.Match(input[startIndex..], $"\\b{pattern}\\b");
        }

        return input;
    }
}