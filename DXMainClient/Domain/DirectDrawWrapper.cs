using ClientCore;
using ClientCore.Extensions;

using Rampastring.Tools;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DTAClient.Domain
{
    /// <summary>
    /// A DirectDraw wrapper option.
    /// </summary>
    public class DirectDrawWrapper
    {
        /// <summary>
        /// Creates a new DirectDrawWrapper instance and parses its configuration
        /// from an INI file.
        /// </summary>
        /// <param name="internalName">The internal name of the renderer.</param>
        /// <param name="iniFile">The file to parse the renderer's options from.</param>
        public DirectDrawWrapper(string internalName, IniFile iniFile)
        {
            InternalName = internalName;
            Parse(iniFile.GetSection(InternalName));
        }

        public string InternalName { get; private set; }
        public string UIName { get; private set; }

        /// <summary>
        /// If not null or empty, windowed mode will be written to an INI key
        /// in this section of the renderer settings file instead
        /// of the regular game settings INI file.
        /// </summary>
        public string WindowedModeSection { get; private set; }

        /// <summary>
        /// If not null or empty, windowed mode will be written to this INI key
        /// in the section defined in <see cref="DirectDrawWrapper.WindowedModeSection"/> 
        /// instead of the regular settings INI file.
        /// </summary>
        public string WindowedModeKey { get; private set; }

        /// <summary>
        /// If not null or empty, the setting that controls whether the game is 
        /// run in borderless windowed mode will be written to this INI key in
        /// the section defined by
        /// <see cref="DirectDrawWrapper.WindowedModeSection"/> instead of the
        /// regular settings INI file.
        /// </summary>
        public string BorderlessWindowedModeKey { get; private set; }

        /// <summary>
        /// If set, borderless mode is enabled if the setting is "false"
        /// and disabled if the setting is "true".
        /// </summary>
        public bool IsBorderlessWindowedModeKeyReversed { get; private set; }

        public bool Hidden { get; private set; }

        /// <summary>
        /// Many ddraw wrappers need qres.dat to set the desktop to 16 bit mode
        /// </summary>
        public bool UseQres { get; private set; } = true;

        /// <summary>
        /// If set to false, the client won't set single-core affinity
        /// to the game executable when this renderer is used.
        /// </summary>
        public bool SingleCoreAffinity { get; private set; } = true;

        /// <summary>
        /// The filename of the configuration INI of the renderer in the game directory.
        /// </summary>
        public string ConfigFileName { get; private set; }

        /// <summary>
        /// Indicates whether this DirectDrawWrapper is a dummy wrapper (i.e. no wrapper).
        /// </summary>
        public bool IsDummy => string.IsNullOrEmpty(ddrawDLLPath);

        private string ddrawDLLPath;
        private string resConfigFileName;
        private List<string> filesToCopy = new List<string>();
        private List<OSVersion> disallowedOSList = new List<OSVersion>();

        /// <summary>
        /// Reads the properties of this DirectDrawWrapper from an INI section.
        /// </summary>
        /// <param name="section">The INI section.</param>
        private void Parse(IniSection section)
        {
            if (section == null)
            {
                Logger.Log("DirectDrawWrapper: Configuration for renderer '" + InternalName + "' not found!");
                return;
            }

            UIName = section.GetStringValue("UIName", "Unnamed renderer");

            if (section.GetBooleanValue("IsDxWnd", false))
            {
                // For backwards compatibility with previous client versions
                WindowedModeSection = "DxWnd";
                WindowedModeKey = "RunInWindow";
                BorderlessWindowedModeKey = "NoWindowFrame";
            }

            WindowedModeSection = section.GetStringValue("WindowedModeSection", WindowedModeSection);
            WindowedModeKey = section.GetStringValue("WindowedModeKey", WindowedModeKey);
            BorderlessWindowedModeKey = section.GetStringValue("BorderlessWindowedModeKey", BorderlessWindowedModeKey);
            IsBorderlessWindowedModeKeyReversed = section.GetBooleanValue("IsBorderlessWindowedModeKeyReversed",
                IsBorderlessWindowedModeKeyReversed);

            if (BorderlessWindowedModeKey != null && WindowedModeSection == null)
            {
                throw new DirectDrawWrapperConfigurationException(
                    "BorderlessWindowedModeKey= is defined for renderer" +
                    $" {InternalName} but WindowedModeSection= is not!");
            }

            Hidden = section.GetBooleanValue("Hidden", false);
            UseQres = section.GetBooleanValue("UseQres", UseQres);
            SingleCoreAffinity = section.GetBooleanValue("SingleCoreAffinity", SingleCoreAffinity);
            ddrawDLLPath = section.GetStringValue("DLLName", string.Empty);
            ConfigFileName = section.GetStringValue("ConfigFileName", string.Empty);
            resConfigFileName = section.GetStringValue("ResConfigFileName", ConfigFileName);

            filesToCopy = section.GetStringValue("AdditionalFiles", string.Empty).Split(
                new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            string[] disallowedOSs = section.GetStringValue("DisallowedOperatingSystems", string.Empty).Split(
                new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string os in disallowedOSs)
            {
                OSVersion disallowedOS = (OSVersion)Enum.Parse(typeof(OSVersion), os.Trim());
                disallowedOSList.Add(disallowedOS);
            }

            if (!string.IsNullOrEmpty(ddrawDLLPath) &&
                !SafePath.GetFile(ProgramConstants.GetBaseResourcePath(), ddrawDLLPath).Exists)
                Logger.Log("DirectDrawWrapper: File specified in DLLPath= for renderer '" + InternalName + "' does not exist!");

            if (!string.IsNullOrEmpty(resConfigFileName) &&
                !SafePath.GetFile(ProgramConstants.GetBaseResourcePath(), resConfigFileName).Exists)
                Logger.Log("DirectDrawWrapper: File specified in ConfigFileName= for renderer '" + InternalName + "' does not exist!");

            foreach (var file in filesToCopy)
            {
                if (!SafePath.GetFile(ProgramConstants.GetBaseResourcePath(), file).Exists)
                    Logger.Log("DirectDrawWrapper: Additional file '" + file + "' for renderer '" + InternalName + "' does not exist!");
            }
        }

        /// <summary>
        /// Returns true if this wrapper is compatible with the given operating
        /// system, otherwise false.
        /// </summary>
        /// <param name="os">The operating system.</param>
        public bool IsCompatibleWithOS(OSVersion os)
        {
            return !disallowedOSList.Contains(os);
        }

        /// <summary>
        /// Applies the renderer's files to the game directory.
        /// </summary>
        public void Apply()
        {
            string ddrawDllSourcePath = SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), ddrawDLLPath);
            string ddrawDllTargetPath = SafePath.CombineFilePath(ProgramConstants.GamePath, "ddraw.dll");

            if (!string.IsNullOrEmpty(ddrawDLLPath))
            {
                FileExtensions.CreateHardLinkFromSource(ddrawDllSourcePath, ddrawDllTargetPath);
                new FileInfo(ddrawDllSourcePath).IsReadOnly = true;
                new FileInfo(ddrawDllTargetPath).IsReadOnly = true;
            }
            else
            {
                if (File.Exists(ddrawDllTargetPath))
                {
                    new FileInfo(ddrawDllTargetPath).IsReadOnly = false;
                    File.Delete(ddrawDllTargetPath);
                }
            }


            if (!string.IsNullOrEmpty(ConfigFileName) && !string.IsNullOrEmpty(resConfigFileName)
                && !SafePath.GetFile(ProgramConstants.GamePath, ConfigFileName).Exists) // Do not overwrite settings
            {
                File.Copy(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), resConfigFileName), SafePath.CombineFilePath(ProgramConstants.GamePath, Path.GetFileName(ConfigFileName)));
            }

            foreach (var file in filesToCopy)
            {
                File.Copy(SafePath.CombineFilePath(ProgramConstants.GetBaseResourcePath(), file), SafePath.CombineFilePath(ProgramConstants.GamePath, Path.GetFileName(file)), true);
            }
        }

        /// <summary>
        /// Call to clean the renderer's files from the game directory.
        /// </summary>
        public void Clean()
        {
            if (!string.IsNullOrEmpty(ConfigFileName))
                SafePath.DeleteFileIfExists(ProgramConstants.GamePath, Path.GetFileName(ConfigFileName));

            foreach (var file in filesToCopy)
                SafePath.DeleteFileIfExists(ProgramConstants.GamePath, Path.GetFileName(file));
        }

        /// <summary>
        /// Checks whether this renderer enables windowed mode through its
        /// own configuration INI file instead of the game settings INI file.
        /// </summary>
        public bool UsesCustomWindowedOption()
        {
            return !string.IsNullOrEmpty(WindowedModeSection) &&
                !string.IsNullOrEmpty(WindowedModeKey);
        }

        // Override == and != operators to compare by InternalName
        public static bool operator ==(DirectDrawWrapper a, DirectDrawWrapper b)
        {
            if (ReferenceEquals(a, b))
                return true;

            if (a is null || b is null)
                return false;

            return a.InternalName == b.InternalName;
        }

        public static bool operator !=(DirectDrawWrapper a, DirectDrawWrapper b) => !(a == b);

        public override bool Equals(object obj)
        {
            if (obj is DirectDrawWrapper other)
            {
                return this == other;
            }
            return false;
        }

        public override int GetHashCode() => InternalName.GetHashCode();
    }

    /// <summary>
    /// An exception that is thrown when configuration for DirectDraw wrapper contains
    /// invalid or unexpected settings / data or required settings / data are missing.
    /// </summary>
    class DirectDrawWrapperConfigurationException : Exception
    {
        public DirectDrawWrapperConfigurationException(string message) : base(message)
        {
        }
    }
}
