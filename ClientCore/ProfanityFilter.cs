using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ClientCore
{
    public class ProfanityFilter
    {
        public IList<string> CensoredWords { get; private set; }

        public ProfanityFilter()
        {
            CensoredWords = new List<string>()
            {
                "cunt*",
                "*nigg*",
                "paki*",
                "shit",
                "fuck*",
                "admin*",
                "allahu*",
                "akbar",
                "twat",
                "cock",
                "pussy",
                "hitler*",
                "anal"
            };
        }

        public ProfanityFilter(IEnumerable<string> censoredWords)
        {
            if (censoredWords == null)
                throw new ArgumentNullException(nameof(censoredWords));
            CensoredWords = new List<string>(censoredWords);
        }

        public bool IsOffensive(string text)
        {
            foreach (string censoredWord in CensoredWords)
            {
                string regularExpression = ToRegexPattern(censoredWord);
                if (Regex.IsMatch(text, regularExpression, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                    return true;
            }
            return false;
        }

        public string CensorText(string text)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));
            string censoredText = text;
            foreach (string censoredWord in CensoredWords)
            {
                string regularExpression = ToRegexPattern(censoredWord);
                censoredText = Regex.Replace(censoredText, regularExpression, StarCensoredMatch,
                  RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }
            return censoredText;
        }

        private static string StarCensoredMatch(Match m)
        {
            string word = m.Captures[0].Value;
            return new string('*', word.Length);
        }

        private string ToRegexPattern(string wildcardSearch)
        {
            string pattern = Regex.Escape(wildcardSearch).Replace(@"\*", ".*?").Replace(@"\?", ".");

            bool startsWithWildcard = wildcardSearch.StartsWith("*") || wildcardSearch.StartsWith("?");
            bool endsWithWildcard = wildcardSearch.EndsWith("*") || wildcardSearch.EndsWith("?");

            if (startsWithWildcard && endsWithWildcard)
                return pattern; 
            
            if (startsWithWildcard)
                return pattern + @"\b";

            if (endsWithWildcard)
                return @"\b" + pattern;

            return @"\b" + pattern + @"\b";
        }
    }
}