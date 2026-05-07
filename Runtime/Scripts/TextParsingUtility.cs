namespace rlmg.Tools.ContentLoading
{
    using System;
    using System.Linq;
    using System.Text.RegularExpressions;

    public static class TextParsingUtility
    {
        public static string ReplaceStrongWithBold(string input)
        {
            string boldPattern0 = @"<strong[^>]*>";
            string boldReplacement0 = "<b>";
            input = Regex.Replace(input, boldPattern0, boldReplacement0);

            string boldPattern1 = @"<\/strong[^>]*>";
            string boldReplacement1 = "</b>";
            input = Regex.Replace(input, boldPattern1, boldReplacement1);

            return input;
        }

        public static string ReplaceEmWithItalics(string input)
        {
            string italicsPattern0 = @"<em[^>]*>";
            string italicsReplacement0 = "<i>";
            input = Regex.Replace(input, italicsPattern0, italicsReplacement0);

            string italicsPattern1 = @"<\/em[^>]*>";
            string italicsReplacement1 = "</i>";
            input = Regex.Replace(input, italicsPattern1, italicsReplacement1);

            return input;
        }

        public static string ReplaceTag(string input, string tag, string replacementTag)
        {
            string tagPattern0 = @"<" + tag + @"[^>]*>";
            string tagReplacement0 = "<" + replacementTag + ">";
            input = Regex.Replace(input, tagPattern0, tagReplacement0);

            string tagPattern1 = @"<\/" + tag + @"[^>]*>";
            string tagReplacement1 = @"</" + replacementTag + ">";
            input = Regex.Replace(input, tagPattern1, tagReplacement1);

            return input;
        }

        public static string EncloseTag(string input, string tag, string enclosure)
        {
            string tagPattern0 = @"(<" + tag + @"[^>]*>)";
            string tagReplacement0 = "<" + enclosure + @">$1";
            input = Regex.Replace(input, tagPattern0, tagReplacement0);

            string tagPattern1 = @"(<\/" + tag + @"[^>]*>)";
            string tagReplacement1 = @"$1</" + enclosure + ">";
            input = Regex.Replace(input, tagPattern1, tagReplacement1);

            return input;
        }

        public static string PrependTag(string input, string tag, string prependage)
        {
            string tagPattern0 = @"(<" + tag + @"[^>]*>)";
            string tagReplacement0 = prependage + @"$1";
            input = Regex.Replace(input, tagPattern0, tagReplacement0);

            return input;
        }

        public static string ClearRichTextTags(string input, string[] exceptions)
        {
            string output = null;

            string pattern = @"(<(?!(" + String.Join("|", exceptions.Select(s => @"\/?" + s).ToArray()) + "))[^>]*>|&nbsp;)";
            string replacement = "";

            output = Regex.Replace(input, pattern, replacement);

            // split into two lists, bodies and headers
            MatchCollection headers;
            string[] bodies;
            string headerPattern = @"<\/?h1[^>]*>";
            headers = Regex.Matches(output, headerPattern);
            bodies = Regex.Split(output, headerPattern);

            return output;
        }

        public static string RemoveDuplicateNewlines(string input)
        {
            string output = input;

            string pattern = @"[\r\n]+";
            string replacement = "\n";
            output = Regex.Replace(output, pattern, replacement);

            return output;
        }

        public static string RemoveNewlineAfterTag(string input, string tag)
        {
            string output = input;

            string pattern = @"(<\/" + tag + @"[^>]*>)([\r\n]+)";
            string replacement = @"$1";
            output = Regex.Replace(output, pattern, replacement);

            return output;
        }
    }
}


