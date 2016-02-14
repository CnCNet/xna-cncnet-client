using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Media;
using System.Diagnostics;
using dtasetup.domain;
using ClientCore;
using ClientGUI;

namespace dtasetup.gui
{
    public partial class LoadMissionForm : MovableForm
    {
        public LoadMissionForm()
        {
            InitializeComponent();
        }

        List<SavedGame> SavedGames = new List<SavedGame>();

        private void LoadMissionForm_Load(object sender, EventArgs e)
        {
            SoundPlayer sPlayer = new SoundPlayer(ProgramConstants.gamepath + ProgramConstants.RESOURCES_DIR + "button.wav");

            this.BackgroundImage = SharedUILogic.LoadImage("loadmissionbg.png");
            this.Font = Utilities.GetFont(DomainController.Instance().GetCommonFont());
            listView1.BackColor = Utilities.GetColorFromString(DomainController.Instance().GetUIAltBackgroundColor());
            this.ForeColor = Utilities.GetColorFromString(DomainController.Instance().GetUIAltColor());
            listView1.ForeColor = this.ForeColor;

            btnLaunch.DefaultImage = SharedUILogic.LoadImage("133pxbtn.png");
            btnLaunch.HoveredImage = SharedUILogic.LoadImage("133pxbtn_c.png");
            btnLaunch.HoverSound = sPlayer;
            btnCancel.DefaultImage = btnLaunch.DefaultImage;
            btnCancel.HoveredImage = btnLaunch.HoveredImage;
            btnCancel.HoverSound = sPlayer;

            this.Icon = Icon.ExtractAssociatedIcon(ProgramConstants.gamepath + ProgramConstants.RESOURCES_DIR + "mainclienticon.ico");

            string[] files = Directory.GetFiles(MainClientConstants.gamepath + "Saved Games\\", "*.SAV", SearchOption.TopDirectoryOnly);

            foreach (string file in files)
            {
                ParseSaveGame(file);
            }

            foreach (SavedGame sg in SavedGames)
            {
                string[] item = new string[] { sg.GUIName, sg.LastModified.ToString() };
                listView1.Items.Add(new ListViewItem(item));
            }

            SharedUILogic.ParseClientThemeIni(this);
        }

        private void ParseSaveGame(string fileName)
        {
            string shortName = Path.GetFileName(fileName);

            try
            {
                Logger.Log("Attempting to read saved game " + shortName);
                BinaryReader br = new BinaryReader(File.Open(fileName, FileMode.Open, FileAccess.Read));

                br.BaseStream.Position = 2256; // 00000980

                string saveGameName = String.Empty;
                // Read name until we encounter two zero-bytes
                bool wasLastByteZero = false;
                while (true)
                {
                    byte characterByte = br.ReadByte();
                    if (characterByte == 0)
                    {
                        if (wasLastByteZero)
                            break;
                        wasLastByteZero = true;
                    }
                    else
                    {
                        wasLastByteZero = false;
                        char character = Convert.ToChar(characterByte);
                        saveGameName = saveGameName + character;
                    }

                    Console.WriteLine();
                }

                SavedGame savedGame = new SavedGame();
                savedGame.FileName = shortName;
                savedGame.GUIName = saveGameName;
                DateTime saveGameModifyDate = File.GetLastWriteTime(fileName);
                savedGame.LastModified = saveGameModifyDate;

                Logger.Log("Saved game " + shortName + " parsed succesfully.");

                // Order saved games according to date and time
                for (int sgId = 0; sgId < SavedGames.Count; )
                {
                    if (SavedGames[sgId].LastModified - saveGameModifyDate < TimeSpan.Zero)
                    {
                        // The parsed save game is newer than the saved game in index sgId
                        SavedGames.Insert(sgId, savedGame);
                        break;
                    }
                    else
                    {
                        if (sgId == SavedGames.Count - 1)
                        {
                            SavedGames.Add(savedGame);
                            break;
                        }

                        sgId++;
                    }
                }

                if (SavedGames.Count == 0)
                    SavedGames.Add(savedGame);

                br.Close();
            }
            catch (Exception ex)
            {
                Logger.Log("An error occured while parsing saved game " + shortName + ":" +
                    ex.Message);
            }
        }

        private void btnLaunch_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count < 1)
            {
                new MsgBoxForm("Please select a saved game from the list.", "No game selected!", MessageBoxButtons.OK).ShowDialog();
                return;
            }

            SavedGame sg = SavedGames[listView1.SelectedIndices[0]];
            Logger.Log("Loading saved game " + sg.FileName);

            if (sg.GUIName == "Multiplayer Game")
            {
                IniFile iniFile = new IniFile(ProgramConstants.gamepath + "Saved Games\\spawn.ini");

                File.Delete(ProgramConstants.gamepath + "spawn.ini");

                IniFile spawnIni = new IniFile(ProgramConstants.gamepath + "spawn.ini");
                spawnIni.SetStringValue("Settings", "SaveGameName", sg.FileName);
                spawnIni.SetBooleanValue("Settings", "LoadSaveGame", true);
                spawnIni.SetBooleanValue("Settings", "SidebarHack", MCDomainController.Instance().GetSidebarHackStatus());
                spawnIni.SetBooleanValue("Settings", "Firestorm", false);
                spawnIni.SetIntValue("Settings", "GameSpeed", MCDomainController.Instance().getGameSpeed());
                spawnIni.SetStringValue("Settings", "Name", iniFile.GetStringValue("Settings", "Name", "Unnamed player"));
                spawnIni.SetIntValue("Settings", "Side", iniFile.GetIntValue("Settings", "Side", 0));
                spawnIni.SetIntValue("Settings", "Color", iniFile.GetIntValue("Settings", "Color", 0));
                spawnIni.SetStringValue("Settings", "CustomLoadScreen", iniFile.GetStringValue("Settings", "CustomLoadScreen", String.Empty));
                spawnIni.SetIntValue("Settings", "Port", iniFile.GetIntValue("Settings", "Port", 0));
                spawnIni.SetBooleanValue("Settings", "Host", iniFile.GetBooleanValue("Settings", "Host", false));

                spawnIni.SetStringValue("Tunnel", "Ip", iniFile.GetStringValue("Tunnel", "Ip", "0.0.0.0"));
                spawnIni.SetIntValue("Tunnel", "Port", iniFile.GetIntValue("Tunnel", "Port", 50000));

                if (iniFile.SectionExists("Other1"))
                {

                }
            }
            else
            {
                File.Delete(MainClientConstants.gamepath + MainClientConstants.SPAWNER_SETTINGS);
                StreamWriter sw = new StreamWriter(MainClientConstants.gamepath + MainClientConstants.SPAWNER_SETTINGS);
                sw.WriteLine("; generated by DTA Launcher");
                sw.WriteLine("[Settings]");
                sw.WriteLine("Scenario=spawnmap.ini");
                sw.WriteLine("SaveGameName=" + sg.FileName);
                sw.WriteLine("LoadSaveGame=Yes");
                sw.WriteLine("SidebarHack=" + MCDomainController.Instance().GetSidebarHackStatus());
                sw.WriteLine("Firestorm=No");
                sw.WriteLine("GameSpeed=" + MCDomainController.Instance().getGameSpeed());
                sw.WriteLine();
                sw.WriteLine();
                sw.WriteLine();
                sw.Close();

                File.Delete(ProgramConstants.gamepath + "spawnmap.ini");
                sw = new StreamWriter(ProgramConstants.gamepath + "spawnmap.ini");
                sw.WriteLine("[Map]");
                sw.WriteLine("Size=0,0,50,50");
                sw.WriteLine("LocalSize=0,0,50,50");
                sw.WriteLine();
                sw.Close();
            }

            Logger.Log("About to launch main executable.");

            if (MCDomainController.Instance().getWindowedStatus())
            {
                Logger.Log("Windowed mode is enabled - using QRes.");
                Process QResProcess = new Process();
                QResProcess.StartInfo.FileName = MainClientConstants.QRES_EXECUTABLE;
                QResProcess.StartInfo.UseShellExecute = false;
                QResProcess.StartInfo.Arguments = "c=16 /R " + "\"" + MainClientConstants.gamepath + 
                    ClientCore.DomainController.Instance().GetGameExecutableName(0) + "\"" + " -SPAWN";
                QResProcess.Start();

                if (Environment.ProcessorCount > 1)
                    QResProcess.ProcessorAffinity = (System.IntPtr)2;
            }
            else
            {
                Process DtaProcess = new Process();
                DtaProcess.StartInfo.FileName = ClientCore.DomainController.Instance().GetGameExecutableName(0);
                DtaProcess.StartInfo.UseShellExecute = false;
                DtaProcess.StartInfo.Arguments = "-SPAWN";
                DtaProcess.Start();

                if (Environment.ProcessorCount > 1)
                    DtaProcess.ProcessorAffinity = (System.IntPtr)2;
            }

            this.DialogResult = DialogResult.OK;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void lbSavedGames_DoubleClick(object sender, EventArgs e)
        {
            btnLaunch.PerformClick();
        }

        private void listView1_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void listView1_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void listView1_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            if (e.ColumnIndex > 0)
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.Gray), new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height));
                e.Graphics.FillRectangle(new SolidBrush(listView1.BackColor), new Rectangle(e.Bounds.X + 1, e.Bounds.Y - 1, e.Bounds.Width - 1, e.Bounds.Height));
            }
            else
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.Gray), new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height));
                e.Graphics.FillRectangle(new SolidBrush(listView1.BackColor), new Rectangle(e.Bounds.X, e.Bounds.Y - 1, e.Bounds.Width, e.Bounds.Height));
            }

            e.Graphics.DrawString(e.Header.Text, new Font(e.Font.FontFamily, e.Font.SizeInPoints, FontStyle.Bold),
                new SolidBrush(this.ForeColor), new RectangleF(e.Bounds.X, e.Bounds.Y + 3.0f, e.Bounds.Width, e.Bounds.Height));
        }
    }
}
