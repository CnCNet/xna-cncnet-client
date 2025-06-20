﻿using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using ClientCore.Extensions;

namespace ClientCore.INIProcessing
{
    public class PreprocessedIniInfo
    {
        public PreprocessedIniInfo(string fileName, string originalHash, string processedHash)
        {
            FileName = fileName;
            OriginalFileHash = originalHash;
            ProcessedFileHash = processedHash;
        }

        public PreprocessedIniInfo(string[] info)
        {
            FileName = info[0];
            OriginalFileHash = info[1];
            ProcessedFileHash = info[2];
        }

        public string FileName { get; }
        public string OriginalFileHash { get; set; }
        public string ProcessedFileHash { get; set; }
    }

    /// <summary>
    /// Handles information on what INI files have been processed by the client.
    /// </summary>
    public class IniPreprocessInfoStore
    {
        private const string StoreIniName = "ProcessedIniInfo.ini";
        private const string ProcessedINIsSection = "ProcessedINIs";

        public List<PreprocessedIniInfo> PreprocessedIniInfos { get; } = new List<PreprocessedIniInfo>();

        /// <summary>
        /// Loads the preprocessed INI information.
        /// </summary>
        public void Load()
        {
            FileInfo processedIniInfoFile = SafePath.GetFile(ProgramConstants.ClientUserFilesPath, "ProcessedIniInfo.ini");

            if (!processedIniInfoFile.Exists)
                return;

            var iniFile = new IniFile(processedIniInfoFile.FullName);
            var keys = iniFile.GetSectionKeys(ProcessedINIsSection);
            foreach (string key in keys)
            {
                string[] values = iniFile.GetStringListValue(ProcessedINIsSection, key, string.Empty);

                if (values.Length != 3)
                {
                    Logger.Log("Failed to parse preprocessed INI info, key " + key);
                    continue;
                }

                // If an INI file no longer exists, it's useless to keep its record
                if (!SafePath.GetFile(ProgramConstants.GamePath, "INI", values[0]).Exists)
                    continue;

                PreprocessedIniInfos.Add(new PreprocessedIniInfo(values));
            }
        }

        /// <summary>
        /// Checks if a (potentially processed) INI file is up-to-date 
        /// or whether it needs to be (re)processed.
        /// </summary>
        /// <param name="fileName">The name of the INI file in its directory.
        /// Do not supply the entire file path.</param>
        /// <returns>True if the INI file is up-to-date, false if it needs to be processed.</returns>
        public bool IsIniUpToDate(string fileName)
        {
            PreprocessedIniInfo info = PreprocessedIniInfos.Find(i => i.FileName == fileName);

            if (info == null)
                return false;

            string processedFileHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, "INI", fileName));
            if (processedFileHash != info.ProcessedFileHash)
                return false;

            string originalFileHash = Utilities.CalculateSHA1ForFile(SafePath.CombineFilePath(ProgramConstants.GamePath, "INI", "Base", fileName));
            if (originalFileHash != info.OriginalFileHash)
                return false;

            return true;
        }

        public void UpsertRecord(string fileName, string originalFileHash, string processedFileHash)
        {
            var existing = PreprocessedIniInfos.Find(i => i.FileName == fileName);
            if (existing == null)
            {
                PreprocessedIniInfos.Add(new PreprocessedIniInfo(fileName, originalFileHash, processedFileHash));
            }
            else
            {
                existing.OriginalFileHash = originalFileHash;
                existing.ProcessedFileHash = processedFileHash;
            }
        }

        public void Write()
        {
            FileInfo processedIniInfoFile = SafePath.GetFile(ProgramConstants.ClientUserFilesPath, "ProcessedIniInfo.ini");

            if (processedIniInfoFile.Exists)
                processedIniInfoFile.Delete();

            IniFile iniFile = new IniFile(processedIniInfoFile.FullName);
            for (int i = 0; i < PreprocessedIniInfos.Count; i++)
            {
                PreprocessedIniInfo info = PreprocessedIniInfos[i];

                iniFile.SetStringValue(ProcessedINIsSection, i.ToString(),
                    string.Join(",", info.FileName, info.OriginalFileHash, info.ProcessedFileHash));
            }
            iniFile.WriteIniFile();
        }
    }
}
