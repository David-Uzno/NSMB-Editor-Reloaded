using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NSMBe5.DSFileSystem;
using NSMBe5.Plugin;

namespace NSMBe5
{
    public partial class LevelChooser
    {
        private void OpenROM()
        {
            string path = "";

            OpenFileDialog openRom = new OpenFileDialog
            {
                Filter = LanguageManager.Get("Filters", "rom")
            };
            if (Properties.Settings.Default.ROMFolder != "")
                openRom.InitialDirectory = Properties.Settings.Default.ROMFolder;
            if (openRom.ShowDialog() == DialogResult.OK)
                path = openRom.FileName;

            if (path == "")
                return;

            LoadROMFromPath(path);
        }

        private void OpenBackups()
        {
            if (Properties.Settings.Default.BackupFiles == "" ||
                MessageBox.Show(LanguageManager.Get("StartForm", "OpenBackups"), LanguageManager.Get("StartForm", "OpenBackupsTitle"), MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            string[] backups = Properties.Settings.Default.BackupFiles.Split(';');
            string path = backups[0];

            if (path == "")
                return;

            for (int l = 1; l < backups.Length; l++)
                ROM.fileBackups.Add(backups[l]);

            LoadROMFromPath(path);
        }

        private void LoadROMFromPath(string path)
        {
            try
            {
                try { CloseROMDependentWindows(); } catch { }
                try { ROM.close(); } catch { }

                NitroROMFilesystem fs = new NitroROMFilesystem(path);
                Properties.Settings.Default.ROMPath = path;
                Properties.Settings.Default.Save();
                Properties.Settings.Default.ROMFolder = Path.GetDirectoryName(path);
                Properties.Settings.Default.Save();

                ROM.load(fs);
                StageObjSettings.Load();

                romLoaded = true;
                LoadROMDependentData();
                UpdateMenuState();

                AddToRecentFiles(path);
                MarkRecentOpened(path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void LoadROMDependentData()
        {
            if (!romLoaded) return;

            filesystemBrowser1.Load(ROM.FS);
            PluginManager.Initialize();

            LoadLevelNames();
            if (ROM.UserInfo != null)
            {
                musicList.Items.Clear();
                musicList.Items.AddRange(ROM.UserInfo.getFullList("Music").ToArray());
            }

            tilesetList1.RefreshList();
            backgroundList1.RefreshList();

            string backupPath = Path.Combine(Application.StartupPath, "Backup");
            if (ROM.fileBackups.Count > 0)
            {
                foreach (string filename in ROM.fileBackups)
                {
                    try
                    {
                        new LevelEditor(new NSMBLevel(LevelSource.getForBackupLevel(filename, backupPath))).Show();
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            Text = "NSMB Editor " + Version.GetString() + " - " + ROM.filename;

            if (!ROM.isNSMBRom)
            {
                if (tabControl1.TabPages.Contains(tabPage2))
                    tabControl1.TabPages.Remove(tabPage2);
                if (tabControl1.TabPages.Contains(tabPage5))
                    tabControl1.TabPages.Remove(tabPage5);
                if (tabControl1.TabPages.Contains(tabPage6))
                    tabControl1.TabPages.Remove(tabPage6);
                nsmbToolsGroupbox.Enabled = false;
                musicSlotsGrp.Enabled = false;
            }
            else
            {
                if (!tabControl1.TabPages.Contains(tabPage2))
                    tabControl1.TabPages.Insert(0, tabPage2);
                if (!tabControl1.TabPages.Contains(tabPage5))
                    tabControl1.TabPages.Insert(1, tabPage5);
                if (!tabControl1.TabPages.Contains(tabPage6))
                    tabControl1.TabPages.Insert(2, tabPage6);
                nsmbToolsGroupbox.Enabled = true;
                musicSlotsGrp.Enabled = true;
            }

            EnableROMDependentControls();
            ApplyLevelListingPreviewMode(false);

            try
            {
                if (tabControl1 != null && tabControl1.TabPages.Contains(tabPage2))
                    tabControl1.SelectedTab = tabPage2;
            }
            catch
            {
            }
        }

        private void CloseROM()
        {
            if (!romLoaded) return;

            CloseROMDependentWindows();

            ROM.close();
            ROM.FS = null;
            ROM.filename = "";
            romLoaded = false;

            levelTreeView.Nodes.Clear();
            allLevelListingItems.Clear();
            activeLevelInternalName = null;
            levelPreviewCache.Clear();
            levelAdvancedPreviewCache.Clear();
            levelAdvancedReducedPreviewCache.Clear();
            UpdateLevelPreview(null);
            musicList.Items.Clear();
            filesystemBrowser1.Load(null);

            tilesetList1.RefreshList();
            backgroundList1.RefreshList();

            Text = "NSMB Editor " + Version.GetString();

            DisableROMDependentControls();
            ApplyLevelListingPreviewMode(false);
            UpdateMenuState();
        }

        private void CloseROMDependentWindows()
        {
            if (imgMgr != null && !imgMgr.IsDisposed)
            {
                imgMgr.Close();
                imgMgr = null;
            }

            if (DataFinderForm != null && !DataFinderForm.IsDisposed)
            {
                DataFinderForm.Close();
                DataFinderForm = null;
            }

            Form[] openForms = Application.OpenForms.Cast<Form>().ToArray();
            foreach (Form form in openForms)
            {
                if (form == this)
                    continue;

                form.Close();
            }
        }

        private void ConnectToNetwork()
        {
            try
            {
                NetFilesystem fs = new NetFilesystem(hostTextBox.Text, Int32.Parse(portTextBox.Text));
                ROM.load(fs);
                StageObjSettings.Load();

                romLoaded = true;
                LoadROMDependentData();
                UpdateMenuState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void EnableROMDependentControls()
        {
            tabControl1.Enabled = true;
            foreach (TabPage tp in tabControl1.TabPages)
                tp.Enabled = true;
        }

        private void DisableROMDependentControls()
        {
            tabControl1.Enabled = true;
            foreach (TabPage tp in tabControl1.TabPages)
            {
                if (tp == tabPage0 || tp == tabPage4)
                    tp.Enabled = true;
                else
                    tp.Enabled = false;
            }
        }

        private void UpdateMenuState()
        {
            closeROMToolStripMenuItem.Enabled = romLoaded;
            openBackupsToolStripMenuItem.Enabled = Properties.Settings.Default.BackupFiles != "";
            recentFilesToolStripMenuItem.Enabled = true;
        }
    }
}
