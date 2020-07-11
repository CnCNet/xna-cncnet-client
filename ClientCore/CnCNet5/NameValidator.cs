using System;
using System.Linq;

namespace ClientCore.CnCNet5
{
    public static class NameValidator
    {
        /// <summary>
        /// Checks if the player's nickname is valid for CnCNet.
        /// </summary>
        /// <returns>Null if the nickname is valid, otherwise a string that tells
        /// what is wrong with the name.</returns>
        public static string IsNameValid(string name)
        {
            var profanityFilter = new ProfanityFilter();

            if (string.IsNullOrEmpty(name))
                return "Please enter a name.";

            if (profanityFilter.IsOffensive(name))
                return "Please enter a name that is less offensive.";

            if (int.TryParse(name.Substring(0, 1), out _))
                return "The first character in the player name cannot be a number.";

            if (name[0] == '-')
                return "The first character in the player name cannot be a dash ( - ).";

            // Check that there are no invalid chars
            char[] allowedCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_[]|\\{}^`".ToCharArray();
            char[] nicknameChars = name.ToCharArray();

            foreach (char nickChar in nicknameChars)
            {
                if (!allowedCharacters.Contains(nickChar))
                {
                    return "Your player name has invalid characters in it." + Environment.NewLine +
                    "Allowed characters are anything from A to Z and numbers.";
                }
            }

            if (name.Length > ClientConfiguration.Instance.MaxNameLength)
                return "Your nickname is too long.";

            return null;
        }

        /// <summary>
        /// Returns player nickname constrained to maximum allowed length and with invalid characters for offline nicknames removed.
        /// Does not check for offensive words or invalid characters for CnCNet.
        /// </summary>
        /// <param name="name">Player nickname.</param>
        /// <returns>Player nickname with invalid offline nickname characters removed and constrained to maximum name length.</returns>
        public static string GetValidOfflineName(string name)
        {
            char[] disallowedCharacters = ",;".ToCharArray();

            string validName = new string(name.Trim().Where(c => !disallowedCharacters.Contains(c)).ToArray());

            if (validName.Length > ClientConfiguration.Instance.MaxNameLength)
                return validName.Substring(0, ClientConfiguration.Instance.MaxNameLength);
            
            return validName;
        }
    }
}
