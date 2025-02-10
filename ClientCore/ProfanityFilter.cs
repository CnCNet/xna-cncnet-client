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
        private readonly Regex _combinedRegex;
        
        public ProfanityFilter()
        {
            var defaultWords = new[]
            {
                "fuck", "shit", "cunt", "nigger", "cock",
                "hitler", "pussy", "akbar", "allahu", "paki"
            };

            //for each bad word
            //      for each letter in the bad word,
            //          match l33tspeak variations (ex: sh!t)
            //          match repeating letters    (ex: sshhhhi!iiit)
            //          ignore hyphens/underscores/whitespace after the letter (ex: s_h_!_t)
            // join into one big, ugly pattern

            var patterns = defaultWords
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

        public bool IsOffensive(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return _combinedRegex.IsMatch(text);
        }

        public string CensorText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            return _combinedRegex.Replace(text, match =>
                new string('*', match.Groups[1].Length)
            );
        }
    }
}