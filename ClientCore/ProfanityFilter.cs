using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ClientCore
{
    public class ProfanityFilter
    {
        private const char CENSOR_CHAR = '*';
        private readonly Regex _combinedRegex;

        /// <summary>
        /// Creates a new profanity filter with a default set of censored words.
        /// </summary>
        /// <param name="extraWords">Any extra words to be considered profane.</param>
        /// 
        public ProfanityFilter(IEnumerable<string> extraWords = null)
        {        
            var defaultWords = new[]
            {
                "fuck", "shit", "cunt", "nigger", "nigga", "niggr", "cock",
                "hitler", "pussy", "akbar", "allahu", "paki", "twat"
            };
            var allWords = defaultWords.Concat(extraWords ?? Enumerable.Empty<string>());

            //for each bad word
            //      for each letter in the bad word,
            //          match l33tspeak variations (ex: sh!t)
            //          match repeating letters    (ex: sshhhhi!iiit)
            //          ignore hyphens/underscores/whitespace after the letter (ex: s_h_!_t)
            // join into one big, ugly pattern

            var patterns = allWords
                .Select(word => string.Join(
                    @"[-_\s]*",  //ignore hyphens/underscores/whitespace after the letter (ex: s_h !_t)
                    word.Select(c => 
                    {
                        switch (char.ToLower(c))
                        {
                            case 'a': return "[aA4@]+"; //in between [] are leetspeak variations.
                            case 'b': return "[bB8]+";  //and a + matches repeating letters
                            case 'e': return "[eE3]+";
                            case 'g': return "[gG69]+";
                            case 'i': return "[iI1!]+";
                            case 'o': return "[oO0]+";
                            case 's': return "[sS5$]+";
                            case 't': return "[tT7]+";
                            case 'z': return "[zZ2]+";
                            default: return $"[{char.ToLower(c)}{char.ToUpper(c)}]+";
                        }
                    })
                ));

            _combinedRegex = new Regex(
                $@"(?i)((?:{string.Join("|", patterns)}))",
                RegexOptions.Compiled
            );
        }

        /// <summary>
        /// Checks if the text contains profanities.
        /// </summary>
        /// <param name="text">The text to be checked for profanities.</param>
        /// 
        public bool IsOffensive(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return _combinedRegex.IsMatch(text);
        }

        /// <summary>
        /// Censors profane words with asterisks in the provided text.
        /// </summary>
        /// <param name="text">The text to be censored.</param>
        /// <param name="respectSetting">Whether or not to abide by the user's chosen setting for censoring profanity.</param>
        /// 
        public string CensorText(string text, bool respectSetting)
        {
            if (respectSetting && !UserINISettings.Instance.FilterProfanity)
            {
                return text;
            }

            if (string.IsNullOrWhiteSpace(text))
                return text;

            return _combinedRegex.Replace(text, match =>
                new string(CENSOR_CHAR, match.Groups[1].Length)
            );
        }
    }
}