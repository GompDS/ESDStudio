using System;
using System.Collections.Generic;

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
}