#nullable enable
using System;
using System.Diagnostics;

using ClientCore;

namespace DTAClient
{
    public static class Dev
    {
        public static bool IsDev { get; private set; } = false;

        public static void Initialize()
        {
            IsDev = IsDev || ClientConfiguration.Instance.ModMode;

#if DEVELOPMENT_BUILD
            IsDev = IsDev || ClientConfiguration.Instance.ShowDevelopmentBuildWarnings;
#endif
        }

        public static void Assert(bool condition, string message)
        {
            if (!IsDev)
            {
                Debug.Assert(condition, message);
                return;
            }

            if (!condition)
            {
                try
                {
                    throw new AssertFailedException($"Assert failed. {message}");
                }
                catch (Exception ex)
                {
                    PreStartup.HandleException(null, ex);
                }
            }
        }
    }
}
