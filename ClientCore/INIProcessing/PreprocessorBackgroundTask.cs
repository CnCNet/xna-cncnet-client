using ClientCore;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientCore.INIProcessing
{
    /// <summary>
    /// Background task for pre-processing INI files.
    /// Singleton.
    /// </summary>
    public class PreprocessorBackgroundTask
    {
        private PreprocessorBackgroundTask()
        {
        }

        private static PreprocessorBackgroundTask _instance;
        public static PreprocessorBackgroundTask Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new PreprocessorBackgroundTask();

                return _instance;
            }
        }

        private Task task;

        public bool IsRunning => !task.IsCompleted;

        public void Run()
        {
            task = Task.Factory.StartNew(() => CheckFiles());
        }

        private void CheckFiles()
        {
            Logger.Log("Starting background processing of INI files.");

            if (!Directory.Exists(ProgramConstants.GamePath + "INI/Base"))
            {
                Logger.Log("/INI/Base does not exist, skipping background processing of INI files.");
                return;
            }

            IniPreprocessInfoStore infoStore = new IniPreprocessInfoStore();
            infoStore.Load();

            IniPreprocessor processor = new IniPreprocessor();

            string[] iniFiles = Directory.GetFiles(ProgramConstants.GamePath + "INI/Base", "*.ini", SearchOption.TopDirectoryOnly);
            iniFiles = Array.ConvertAll(iniFiles, s => Path.GetFileName(s));

            int processedCount = 0;

            foreach (string fileName in iniFiles)
            {
                if (!infoStore.IsIniUpToDate(fileName))
                {
                    Logger.Log("INI file " + fileName + " is not processed or outdated, re-processing it.");

                    string sourcePath = $"{ProgramConstants.GamePath}INI/Base/{fileName}";
                    string destinationPath = $"{ProgramConstants.GamePath}INI/{fileName}";

                    processor.ProcessIni(sourcePath, destinationPath);

                    string sourceHash = Utilities.CalculateSHA1ForFile(sourcePath);
                    string destinationHash = Utilities.CalculateSHA1ForFile(destinationPath);
                    infoStore.UpsertRecord(fileName, sourceHash, destinationHash);
                    processedCount++;
                }
                else
                {
                    Logger.Log("INI file " + fileName + " is up to date.");
                }
            }

            if (processedCount > 0)
            {
                Logger.Log("Writing preprocessed INI info store.");
                infoStore.Write();
            }

            Logger.Log("Ended background processing of INI files.");
        }
    }
}
