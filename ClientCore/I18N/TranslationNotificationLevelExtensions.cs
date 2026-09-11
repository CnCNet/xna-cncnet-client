#nullable enable
using System;

using ClientCore.Extensions;

namespace ClientCore.I18N;

public static class TranslationNotificationLevelExtensions
{
    extension(TranslationNotificationLevel)
    {
        public static TranslationNotificationLevel FromInt(int value)
        {
            if (!Enum.IsDefined(typeof(TranslationNotificationLevel), value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), string.Format(
                        "Invalid value for TranslationNotificationLevel: {0}".L10N("Client:ClientCore:InvalidTranslationNotificationLevelValue"), value));
            }

            return (TranslationNotificationLevel)value;
        }
    }

    extension(TranslationNotificationLevel thisLevel)
    {
        public int ToInt() => (int)thisLevel;
    }
}
