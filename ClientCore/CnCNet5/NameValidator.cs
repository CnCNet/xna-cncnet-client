using System;
using System.Linq;
using ClientCore.Extensions;

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
                return "Please enter a name.".L10N("Client:ClientCore:EnterAName");

            if (profanityFilter.IsOffensive(name))
                return "Please enter a name that is less offensive.".L10N("Client:ClientCore:NameOffensive");

            if (int.TryParse(name.Substring(0, 1), out _))
                return "The first character in the player name cannot be a number.".L10N("Client:ClientCore:NameFirstIsNumber");

            if (name[0] == '-')
                return "The first character in the player name cannot be a dash ( - ).".L10N("Client:ClientCore:NameFirstIsDash");

            // Check that there are no invalid chars
            char[] allowedCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_[]|\\{}^`".ToCharArray();
            char[] nicknameChars = name.ToCharArray();

            foreach (char nickChar in nicknameChars)
            {
                if (!allowedCharacters.Contains(nickChar))
                {
                    return "Your player name has invalid characters in it.".L10N("Client:ClientCore:NameInvalidChar1") + Environment.NewLine +
                    "Allowed characters are anything from A to Z and numbers.".L10N("Client:ClientCore:NameInvalidChar2");
                }
            }

            if (name.Length > ClientConfiguration.Instance.MaxNameLength)
                return "Your nickname is too long.".L10N("Client:ClientCore:NameTooLong");

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

        /// <summary>
        /// Checks if a game name is valid for CnCNet.
        /// </summary>
        /// <param name="gameName">Game name.</param>
        /// <returns>Null if the game name is valid, otherwise a string that tells
        /// what is wrong with the name.</returns>
        public static string IsGameNameValid(string gameName)
        {

            if (string.IsNullOrEmpty(gameName))
            {
                return "Please enter a game name.".L10N("Client:Main:GameNameMissing");
            }

            char[] disallowedCharacters = { ',', ';' };
            if (gameName.IndexOfAny(disallowedCharacters) != -1)
            {
                return "Game name contains disallowed characters.".L10N("Client:Main:GameNameDisallowedChars");
            }

            if (gameName.Length > 23)
            {
                return "Game name is too long.".L10N("Client:Main:GameNameTooLong");
            }

            if (new ProfanityFilter().IsOffensive(gameName))
            {
                return "Please enter a less offensive game name.".L10N("Client:Main:GameNameOffensiveText");
            }

            return null;
        }
    }
}
