using System;
using System.Linq;

using ClientCore;
using ClientCore.Extensions;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    public enum NameValidationError
    {
        None = 0,
        EmptyName,
        OffensiveName,
        FirstCharacterIsNumber,
        FirstCharacterIsHyphen,
        InvalidCharacters,
        TooLong
    }

    public static class NameValidator
    {
        /// <summary>
        /// Gets the localized error message for a player name validation error.
        /// </summary>
        /// <param name="error">The validation error.</param>
        /// <returns>Localized error message, or null if the error is None.</returns>
        public static string GetLocalizedPlayerNameErrorMessage(NameValidationError error)
        {
            switch (error)
            {
                case NameValidationError.None:
                    return null;
                case NameValidationError.EmptyName:
                    return "Please enter a name.".L10N("Client:ClientCore:EnterAName");
                case NameValidationError.OffensiveName:
                    return "Please enter a name that is less offensive.".L10N("Client:ClientCore:NameOffensive");
                case NameValidationError.FirstCharacterIsNumber:
                    return "The first character in the player name cannot be a number.".L10N("Client:ClientCore:NameFirstIsNumber");
                case NameValidationError.FirstCharacterIsHyphen:
                    return "The first character in the player name cannot be a hyphen ( - ).".L10N("Client:ClientCore:NameFirstIsHyphen");
                case NameValidationError.InvalidCharacters:
                    return "Your player name has invalid characters in it.".L10N("Client:ClientCore:NameInvalidChar1") + Environment.NewLine +
                           "Allowed characters are anything from A to Z and numbers.".L10N("Client:ClientCore:NameInvalidChar2");
                case NameValidationError.TooLong:
                    return "Your nickname is too long.".L10N("Client:ClientCore:NameTooLong");
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the localized error message for a game name validation error.
        /// </summary>
        /// <param name="error">The validation error.</param>
        /// <returns>Localized error message, or null if the error is None.</returns>
        public static string GetLocalizedGameNameErrorMessage(NameValidationError error)
        {
            switch (error)
            {
                case NameValidationError.None:
                    return null;
                case NameValidationError.EmptyName:
                    return "Please enter a game name.".L10N("Client:Main:PleaseEnterGameName");
                case NameValidationError.OffensiveName:
                    return "Please enter a less offensive game name.".L10N("Client:Main:GameNameOffensiveText");
                default:
                    return null;
            }
        }

        /// <summary>
        /// Checks if the player's nickname is valid for CnCNet.
        /// </summary>
        /// <param name="name">The player name to validate.</param>
        /// <param name="localizedErrorMessage">The localized error message if validation fails, otherwise null.</param>
        /// <returns>NameValidationError.None if the nickname is valid, otherwise the specific validation error.</returns>
        public static NameValidationError IsNameValid(string name, out string localizedErrorMessage)
        {
            var profanityFilter = new ProfanityFilter();

            if (string.IsNullOrEmpty(name))
            {
                localizedErrorMessage = GetLocalizedPlayerNameErrorMessage(NameValidationError.EmptyName);
                return NameValidationError.EmptyName;
            }

            if (profanityFilter.IsOffensive(name))
            {
                localizedErrorMessage = GetLocalizedPlayerNameErrorMessage(NameValidationError.OffensiveName);
                return NameValidationError.OffensiveName;
            }

            if (int.TryParse(name.Substring(0, 1), out _))
            {
                localizedErrorMessage = GetLocalizedPlayerNameErrorMessage(NameValidationError.FirstCharacterIsNumber);
                return NameValidationError.FirstCharacterIsNumber;
            }

            if (name[0] == '-')
            {
                localizedErrorMessage = GetLocalizedPlayerNameErrorMessage(NameValidationError.FirstCharacterIsHyphen);
                return NameValidationError.FirstCharacterIsHyphen;
            }

            // Check that there are no invalid chars
            char[] allowedCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_[]|\\{}^`".ToCharArray();
            char[] nicknameChars = name.ToCharArray();

            foreach (char nickChar in nicknameChars)
            {
                if (!allowedCharacters.Contains(nickChar))
                {
                    localizedErrorMessage = GetLocalizedPlayerNameErrorMessage(NameValidationError.InvalidCharacters);
                    return NameValidationError.InvalidCharacters;
                }
            }

            if (name.Length > ClientConfiguration.Instance.MaxNameLength)
            {
                localizedErrorMessage = GetLocalizedPlayerNameErrorMessage(NameValidationError.TooLong);
                return NameValidationError.TooLong;
            }

            localizedErrorMessage = null;
            return NameValidationError.None;
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
        /// Checks if a lobby room name is valid.
        /// </summary>
        /// <param name="name">The lobby name to validate.</param>
        /// <param name="localizedErrorMessage">The localized error message if validation fails, otherwise null.</param>
        /// <returns>NameValidationError.None if the name is valid, otherwise the specific validation error.</returns>
        public static NameValidationError IsGameNameValid(string name, out string localizedErrorMessage)
        {
            var profanityFilter = new ProfanityFilter();

            if (string.IsNullOrEmpty(name))
            {
                localizedErrorMessage = GetLocalizedGameNameErrorMessage(NameValidationError.EmptyName);
                return NameValidationError.EmptyName;
            }

            if (profanityFilter.IsOffensive(name))
            {
                localizedErrorMessage = GetLocalizedGameNameErrorMessage(NameValidationError.OffensiveName);
                return NameValidationError.OffensiveName;
            }

            localizedErrorMessage = null;
            return NameValidationError.None;
        }

        /// <summary>
        /// Sanitizes a lobby name by removing invalid characters.
        /// </summary>
        /// <param name="name">The lobby room name to sanitize.</param>
        /// <returns>The sanitized lobby room name.</returns>
        public static string GetSanitizedGameName(string name)
        {
            // semicolons are used as separators in the protocol
            return name.Replace(";", string.Empty).Trim();
        }
    }
}
