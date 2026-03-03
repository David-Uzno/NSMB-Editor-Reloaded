/*
*   This file is part of NSMB Editor 5.
*
*   NSMB Editor 5 is free software: you can redistribute it and/or modify
*   it under the terms of the GNU General Public License as published by
*   the Free Software Foundation, either version 3 of the License, or
*   (at your option) any later version.
*
*   NSMB Editor 5 is distributed in the hope that it will be useful,
*   but WITHOUT ANY WARRANTY; without even the implied warranty of
*   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*   GNU General Public License for more details.
*
*   You should have received a copy of the GNU General Public License
*   along with NSMB Editor 5.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;

using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using NSMBe5.DSFileSystem;
using NSMBe5.Patcher;
using System.Diagnostics;
using System.Drawing;
using NSMBe5.Plugin;
using System.Linq;
using NSMBe5.TilemapEditor;
using System.Runtime.InteropServices;

namespace NSMBe5 {
    public partial class LevelChooser : Form
    {
        private const int SB_HORZ = 0;

        [DllImport("user32.dll")]
        private static extern bool ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);

        public static ImageManagerWindow imgMgr;
        public TextInputForm textForm = new TextInputForm();
        // init has to be used because Winforms is setting the value of autoBackupTime before the form loads
        //   This causes it to be saved in the settings before the settings value is loaded.
        public bool init = false;
        private bool romLoaded = false;
        private Dictionary<string, string> projectDisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            // Currently loaded recent file path — only this card's title is bold
            private string currentLoadedRecentFile = null;
            private static string GetRelativeTimeString(DateTime dt)
            {
                var span = DateTime.Now - dt;
                if (span.TotalSeconds < 60)
                    return LanguageManager.Get("LevelChooser", "Time_AfewSeconds") ?? "A few seconds ago";

                if (span.TotalMinutes < 60)
                {
                    int m = Math.Max(1, (int)span.TotalMinutes);
                    string key = m == 1 ? "Time_Minute_Singular" : "Time_Minute_Plural";
                    string fmt = LanguageManager.Get("LevelChooser", key);
                    if (string.IsNullOrEmpty(fmt))
                        fmt = m == 1 ? "{0} minute ago" : "{0} minutes ago";
                    return string.Format(fmt, m);
                }

                if (span.TotalHours < 24)
                {
                    int h = Math.Max(1, (int)span.TotalHours);
                    string key = h == 1 ? "Time_Hour_Singular" : "Time_Hour_Plural";
                    string fmt = LanguageManager.Get("LevelChooser", key);
                    if (string.IsNullOrEmpty(fmt))
                        fmt = h == 1 ? "{0} hour ago" : "{0} hours ago";
                    return string.Format(fmt, h);
                }

                if (span.TotalDays < 30)
                {
                    int d = Math.Max(1, (int)span.TotalDays);
                    string key = d == 1 ? "Time_Day_Singular" : "Time_Day_Plural";
                    string fmt = LanguageManager.Get("LevelChooser", key);
                    if (string.IsNullOrEmpty(fmt))
                        fmt = d == 1 ? "{0} day ago" : "{0} days ago";
                    return string.Format(fmt, d);
                }

                if (span.TotalDays < 365)
                {
                    int mo = Math.Max(1, (int)(span.TotalDays / 30));
                    string key = mo == 1 ? "Time_Month_Singular" : "Time_Month_Plural";
                    string fmt = LanguageManager.Get("LevelChooser", key);
                    if (string.IsNullOrEmpty(fmt))
                        fmt = mo == 1 ? "{0} month ago" : "{0} months ago";
                    return string.Format(fmt, mo);
                }

                int y = Math.Max(1, (int)(span.TotalDays / 365));
                string ykey = y == 1 ? "Time_Year_Singular" : "Time_Year_Plural";
                string yfmt = LanguageManager.Get("LevelChooser", ykey);
                if (string.IsNullOrEmpty(yfmt))
                    yfmt = y == 1 ? "{0} year ago" : "{0} years ago";
                return string.Format(yfmt, y);
            }
            private void UpdateCardModifiedLabel(string filePath)
            {
                if (this.projectsPanel == null) return;
                foreach (Control c in this.projectsPanel.Controls)
                {
                    if (c is Panel panel && panel.Tag is string fp && fp == filePath)
                    {
                        foreach (Control child in panel.Controls)
                        {
                            if (child is Label lbl && lbl.Tag is string t && t == "modifiedLbl")
                            {
                                try
                                {
                                    lbl.Text = GetRelativeTimeString(System.IO.File.GetLastWriteTime(filePath));
                                }
                                catch { }
                                return;
                            }
                        }
                    }
                }
            }

        public static void ShowImgMgr()
        {
            if (imgMgr == null || imgMgr.IsDisposed)
                imgMgr = new ImageManagerWindow();

            imgMgr.Show();
        }

        public LevelChooser()
        {
            InitializeComponent();
        }

        private void LevelChooser_Load(object sender, EventArgs e)
		{
			dlpCheckBox.Checked = Properties.Settings.Default.dlpMode;
            chkAutoBackup.Checked = Properties.Settings.Default.AutoBackup > 0;
            if (chkAutoBackup.Checked)
                autoBackupTime.Value = Properties.Settings.Default.AutoBackup;
            init = true;

            // Check if ROM is loaded
            romLoaded = ROM.FS != null;

            if (romLoaded)
            {
                LoadROMDependentData();
            }
            else
            {
                // Set title without ROM name
                Text = "NSMB Editor " + Version.GetString();
                DisableROMDependentControls();
            }

            LanguageManager.ApplyToContainer(this, "LevelChooser");
            openROMDialog.Filter = LanguageManager.Get("Filters", "rom");
            importLevelDialog.Filter = LanguageManager.Get("Filters", "level");
            exportLevelDialog.Filter = LanguageManager.Get("Filters", "level");
            openPatchDialog.Filter = LanguageManager.Get("Filters", "patch");
            savePatchDialog.Filter = LanguageManager.Get("Filters", "patch");
            openTextFileDialog.Filter = LanguageManager.Get("Filters", "text");
            saveTextFileDialog.Filter = LanguageManager.Get("Filters", "text");
            
            // Load stored project display names and recent files
            LoadProjectDisplayNames();
            LoadRecentFiles();
            
            //Get Language Files
            string langDir = System.IO.Path.Combine(Application.StartupPath, "Languages");
            if (System.IO.Directory.Exists(langDir)) {
                string[] files = System.IO.Directory.GetFiles(langDir);
                for (int l = 0; l < files.Length; l++) {
                    if (files[l].EndsWith(".ini")) {
                        int startPos = files[l].LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1;
                        languagesComboBox.Items.Add(files[l].Substring(startPos, files[l].LastIndexOf('.') - startPos));
                    }
                }
            }
            languagesComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            languagesComboBox.SelectedIndexChanged -= new EventHandler(LanguagesComboBox_SelectedIndexChanged);
            languagesComboBox.SelectedItem = Properties.Settings.Default.LanguageFile;
            languagesComboBox.SelectedIndexChanged += new EventHandler(LanguagesComboBox_SelectedIndexChanged);

            string[] codePatchingMethods =
            {
                "NSMBe",
                "Fireflower",
                "NCPatcher"
            };

            patchMethodComboBox.Items.AddRange(codePatchingMethods);
            patchMethodComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            patchMethodComboBox.SelectedIndexChanged -= new EventHandler(PatchMethodComboBox_SelectedIndexChanged);
            patchMethodComboBox.SelectedIndex = Properties.Settings.Default.CodePatchingMethod;
            patchMethodComboBox.SelectedIndexChanged += new EventHandler(PatchMethodComboBox_SelectedIndexChanged);

            fontTextBox.Text = Properties.Settings.Default.UIFont;
            Program.ApplyFontToControls(Controls);
            
            versionLabel.Text = "NSMB Editor " + Version.GetString() + " " + Properties.Resources.BuildDate.Trim();
            Icon = Properties.Resources.nsmbe;
            
            UpdateMenuState();
            Activate();
        }

        private void LoadLevelNames()
        {
            List<string> LevelNames = LanguageManager.GetList("LevelNames");

            TreeNode WorldNode = null;
            TreeNode LevelNode;
            TreeNode AreaNode;
            for (int NameIdx = 0; NameIdx < LevelNames.Count; NameIdx++) {
                LevelNames[NameIdx] = LevelNames[NameIdx].Trim();
                if (LevelNames[NameIdx] == "") continue;
                if (LevelNames[NameIdx][0] == '-') {
                    // Create a world
                    string[] ParseWorld = LevelNames[NameIdx].Substring(1).Split('|');
                    WorldNode = levelTreeView.Nodes.Add("ln" + NameIdx.ToString(), ParseWorld[0]);
                } else {
                    // Create a level
                    string[] ParseLevel = LevelNames[NameIdx].Split('|');
                    int AreaCount = ParseLevel.Length - 1;
                    if (AreaCount == 1) {
                        // One area only; no need for a subfolder here
                        LevelNode = WorldNode.Nodes.Add("ln" + NameIdx.ToString(), ParseLevel[0]);
                        LevelNode.Tag = ParseLevel[1];
                    } else {
                        // Create a subfolder
                        LevelNode = WorldNode.Nodes.Add("ln" + NameIdx.ToString(), ParseLevel[0]);
                        for (int AreaIdx = 1; AreaIdx <= AreaCount; AreaIdx++) {
                            AreaNode = LevelNode.Nodes.Add("ln" + NameIdx.ToString() + "a" + AreaIdx.ToString(), LanguageManager.Get("LevelChooser", "Area") + " " + AreaIdx.ToString());
                            AreaNode.Tag = ParseLevel[AreaIdx];
                        }
                    }
                }
            }
        }

        private void UpdateCardOpenedState()
        {
            if (this.projectsPanel == null) return;
            foreach (Control c in this.projectsPanel.Controls)
            {
                if (c is Panel panel && panel.Tag is string fp)
                {
                    foreach (Control child in panel.Controls)
                    {
                        if (child is Label lbl && lbl.Tag is string t && t == "nameLbl")
                        {
                            try
                            {
                                if (!string.IsNullOrEmpty(currentLoadedRecentFile) && string.Equals(fp, currentLoadedRecentFile, StringComparison.OrdinalIgnoreCase))
                                    lbl.Font = new Font(lbl.Font, FontStyle.Bold);
                                else
                                    lbl.Font = new Font(lbl.Font, FontStyle.Regular);
                            }
                            catch { }
                        }
                    }
                }
            }
        }

        private void MarkRecentOpened(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            currentLoadedRecentFile = filePath;
            UpdateCardOpenedState();
        }

        private void LevelTreeView_AfterSelect(object sender, TreeViewEventArgs e) {
            bool enabled = e.Node.Tag != null;
            importLevelButton.Enabled = enabled;
            exportLevelButton.Enabled = enabled;
            editLevelButton.Enabled = enabled;
            hexEditLevelButton.Enabled = enabled;
            importClipboard.Enabled = enabled;
            exportClipboard.Enabled = enabled;
        }

        private void EditLevelButton_Click(object sender, EventArgs e)
        {
            // Make a caption
            string EditorCaption = "";

            if (levelTreeView.SelectedNode.Parent.Parent == null) {
                EditorCaption += levelTreeView.SelectedNode.Text;
            } else {
                EditorCaption += levelTreeView.SelectedNode.Parent.Text + ", " + levelTreeView.SelectedNode.Text;
            }

            // Open it
            try
            {
                LevelEditor NewEditor = new LevelEditor(new NSMBLevel(new InternalLevelSource((string)levelTreeView.SelectedNode.Tag, EditorCaption)));
                NewEditor.Show();
            }
            catch (AlreadyEditingException)
            {
                MessageBox.Show(LanguageManager.Get("Errors", "Level"));
            }                
        }

        private void HexEditLevelButton_Click(object sender, EventArgs e) {
            if (levelTreeView.SelectedNode == null) return;

            // Make a caption
            string EditorCaption = LanguageManager.Get("General", "EditingSomething") + " ";

            if (levelTreeView.SelectedNode.Parent.Parent == null) {
                EditorCaption += levelTreeView.SelectedNode.Text;
            } else {
                EditorCaption += levelTreeView.SelectedNode.Parent.Text + ", " + levelTreeView.SelectedNode.Text;
            }

            // Open it
            try
            {
                LevelHexEditor NewEditor = new LevelHexEditor((string)levelTreeView.SelectedNode.Tag)
                {
                    Text = EditorCaption
                };
                NewEditor.Show();
            }
            catch (AlreadyEditingException)
            {
                MessageBox.Show(LanguageManager.Get("Errors", "Level"));
            }                
        }

        private void LevelTreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e) {
            if (e.Node.Tag != null && editLevelButton.Enabled == true) {
                EditLevelButton_Click(null, null);
            }
        }

        private void ImportLevelButton_Click(object sender, EventArgs e) {
            if (levelTreeView.SelectedNode == null) return;

            // Figure out what file to import
            if (importLevelDialog.ShowDialog() == DialogResult.Cancel)
                return;

            // Get the files
            string LevelFilename = (string)levelTreeView.SelectedNode.Tag;
            DSFileSystem.File LevelFile = ROM.getLevelFile(LevelFilename);
            DSFileSystem.File BGFile = ROM.getBGDatFile(LevelFilename);

            // Load it
            try
            {
                ExternalLevelSource level = new ExternalLevelSource(importLevelDialog.FileName);
                level.level.Import(LevelFile, BGFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ExportLevelButton_Click(object sender, EventArgs e) {
            if (levelTreeView.SelectedNode == null) return;

            // Figure out what file to export to
            if (exportLevelDialog.ShowDialog() == DialogResult.Cancel)
                return;

            // Get the files
            string LevelFilename = (string)levelTreeView.SelectedNode.Tag;
            DSFileSystem.File LevelFile = ROM.getLevelFile(LevelFilename);
            DSFileSystem.File BGFile = ROM.getBGDatFile(LevelFilename);

            // Load it
            FileStream fs = new FileStream(exportLevelDialog.FileName, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);
            new ExportedLevel(LevelFile, BGFile).Write(bw);
            bw.Close();
        }

        private void OpenLevel_Click(object sender, EventArgs e)
        {
            if (importLevelDialog.ShowDialog() == System.Windows.Forms.DialogResult.Cancel)
                return;
            try {
                new LevelEditor(new NSMBLevel(new ExternalLevelSource(importLevelDialog.FileName))).Show();
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        private void ImportClipboard_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(LanguageManager.Get("LevelChooser", "replaceclipboard"), LanguageManager.Get("LevelChooser", "replaceclipboardtitle"), MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
                return;
            try
            {
                string LevelFilename = (string)levelTreeView.SelectedNode.Tag;
                DSFileSystem.File LevelFile = ROM.getLevelFile(LevelFilename);
                DSFileSystem.File BGFile = ROM.getBGDatFile(LevelFilename);
                ClipboardLevelSource level = new ClipboardLevelSource();
                level.level.Import(LevelFile, BGFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(LanguageManager.Get("LevelChooser", "clipinvalidlevel"));
            }
        }

        private void ExportClipboard_Click(object sender, EventArgs e)
        {
            string LevelFilename = (string)levelTreeView.SelectedNode.Tag;
            DSFileSystem.File LevelFile = ROM.getLevelFile(LevelFilename);
            DSFileSystem.File BGFile = ROM.getBGDatFile(LevelFilename);

            ByteArrayInputStream strm = new ByteArrayInputStream(new byte[0]);
            BinaryWriter bw = new BinaryWriter(strm);

            new ExportedLevel(LevelFile, BGFile).Write(bw);
            ClipboardLevelSource.copyData(strm.getData());
            bw.Close();
        }

        private void OpenClipboard_Click(object sender, EventArgs e)
        {
            try
            {
                new LevelEditor(new NSMBLevel(new ClipboardLevelSource())).Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(LanguageManager.Get("LevelChooser", "clipinvalidlevel"));
            }
        }

        private DataFinder DataFinderForm;

        private void DataFinderButton_Click(object sender, EventArgs e) {
            if (DataFinderForm == null || DataFinderForm.IsDisposed) {
                DataFinderForm = new DataFinder();
            }

            DataFinderForm.Show();
            DataFinderForm.Activate();
        }


        /**
         * PATCH FILE FORMAT
         * 
         * - String "NSMBe5 Exported Patch"
         * - Some files (see below)
         * - byte 0
         * 
         * STRUCTURE OF A FILE
         * - byte 1
         * - File name as a string
         * - File ID as ushort (to check for different versions, only gives a warning)
         * - File length as uint
         * - File contents as byte[]
         */

        const string oldPatchHeader = "NSMBe4 Exported Patch";
        public const string patchHeader = "NSMBe5 Exported Patch";

        private void PatchExport_Click(object sender, EventArgs e)
        {
            //output to show to the user
            bool differentRomsWarning = false; // tells if we have shown the warning
            int fileCount = 0;

            //load the original rom
            MessageBox.Show(LanguageManager.Get("Patch", "SelectROM"), LanguageManager.Get("Patch", "Export"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (openROMDialog.ShowDialog() == DialogResult.Cancel)
                return;
            NitroROMFilesystem origROM = new NitroROMFilesystem(openROMDialog.FileName);

            //open the output patch
            MessageBox.Show(LanguageManager.Get("Patch", "SelectLocation"), LanguageManager.Get("Patch", "Export"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            if (savePatchDialog.ShowDialog() == DialogResult.Cancel)
                return;

            FileStream fs = new FileStream(savePatchDialog.FileName, FileMode.Create, FileAccess.Write, FileShare.None);
            
            BinaryWriter bw = new BinaryWriter(fs);
            bw.Write(patchHeader);

            //DO THE PATCH!!
            ProgressWindow progress = new ProgressWindow(LanguageManager.Get("Patch", "ExportProgressTitle"));
            progress.Show();
            progress.SetMax(ROM.FS.allFiles.Count);
            int progVal = 0;
            MessageBox.Show(LanguageManager.Get("Patch", "StartingPatch"), LanguageManager.Get("Patch", "Export"), MessageBoxButtons.OK, MessageBoxIcon.Information);

            foreach (NSMBe5.DSFileSystem.File f in ROM.FS.allFiles)
            {
                if (f.isSystemFile) continue;

                Console.Out.WriteLine("Checking " + f.name);
                progress.SetCurrentAction(string.Format(LanguageManager.Get("Patch", "ComparingFile"), f.name));

                NSMBe5.DSFileSystem.File orig = origROM.getFileByName(f.name);
                //check same version
                if(orig == null)
                {
                    new ErrorMSGBox("", "", "In this case it is recommended that you continue.", "This ROM has more files than the original clean ROM or a file was renamed!\n\nPlease make an XDelta patch instead.\n\nExport will end now.").ShowDialog();
                    bw.Write((byte)0);
                    bw.Close();
                    origROM.close();
                    progress.SetCurrentAction("");
                    progress.WriteLine(string.Format(LanguageManager.Get("Patch", "ExportReady"), fileCount));
                    return;
                }
                else if(!differentRomsWarning && f.id != orig.id)
                {
                    if (MessageBox.Show(LanguageManager.Get("Patch", "ExportDiffVersions"), LanguageManager.Get("General", "Warning"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                        differentRomsWarning = true;
                    else
                    {
                        fs.Close();
                        return;
                    }
                }

                byte[] oldFile = orig.getContents();
                byte[] newFile = f.getContents();

                if (!ArrayEqual(oldFile, newFile))
                {
                    //include file in patch
                    string fileName = orig.name;
                    Console.Out.WriteLine("Including: " + fileName);
                    progress.WriteLine(string.Format(LanguageManager.Get("Patch", "IncludedFile"), fileName));
                    fileCount++;

                    bw.Write((byte)1);
                    bw.Write(fileName);
                    bw.Write((ushort)f.id);
                    bw.Write((uint)newFile.Length);
                    bw.Write(newFile, 0, newFile.Length);
                }
                progress.setValue(++progVal);
            }
            bw.Write((byte)0);
            bw.Close();
            origROM.close();
            progress.SetCurrentAction("");
            progress.WriteLine(string.Format(LanguageManager.Get("Patch", "ExportReady"), fileCount));
        }

        public static bool ArrayEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i])
                    return false;

            return true;
        }

        private void PatchImport_Click(object sender, EventArgs e)
        {
            //output to show to the user
            bool differentRomsWarning = false; // tells if we have shown the warning
            int fileCount = 0;

            //open the input patch
            if (openPatchDialog.ShowDialog() == DialogResult.Cancel)
                return;

            FileStream fs = new FileStream(openPatchDialog.FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            BinaryReader br = new BinaryReader(fs);

            string header = br.ReadString();
            if (!(header == patchHeader || header == oldPatchHeader))
            {
                MessageBox.Show(
                    LanguageManager.Get("Patch", "InvalidFile"),
                    LanguageManager.Get("Patch", "Unreadable"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                br.Close();
                return;
            }


            ProgressWindow progress = new ProgressWindow(LanguageManager.Get("Patch", "ImportProgressTitle"));
            progress.Show();

            byte filestartByte = br.ReadByte();
            try
            {
                while (filestartByte == 1)
                {
                    string fileName = br.ReadString();
                    progress.WriteLine(string.Format(LanguageManager.Get("Patch", "ReplacingFile"), fileName));
                    ushort origFileID = br.ReadUInt16();
                    NSMBe5.DSFileSystem.File f = ROM.FS.getFileByName(fileName);
                    uint length = br.ReadUInt32();

                    byte[] newFile = new byte[length];
                    br.Read(newFile, 0, (int)length);
                    filestartByte = br.ReadByte();

                    if (f != null)
                    {
                        ushort fileID = (ushort)f.id;

                        if (!differentRomsWarning && origFileID != fileID)
                        {
                            MessageBox.Show(LanguageManager.Get("Patch", "ImportDiffVersions"), LanguageManager.Get("General", "Warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            differentRomsWarning = true;
                        }
                        if (!f.isSystemFile)
                        {
                            Console.Out.WriteLine("Replace " + fileName);
                            f.beginEdit(this);
                            f.replace(newFile, this);
                            f.endEdit(this);
                        }
                        fileCount++;
                    }
                }
            }
            catch (AlreadyEditingException)
            {
                MessageBox.Show(string.Format(LanguageManager.Get("Patch", "Error"), fileCount), LanguageManager.Get("General", "Completed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            br.Close();
            MessageBox.Show(string.Format(LanguageManager.Get("Patch", "ImportReady"), fileCount), LanguageManager.Get("General", "Completed"), MessageBoxButtons.OK, MessageBoxIcon.Information);
//            progress.Close();
        }

        private void MpPatch_Click(object sender, EventArgs e)
        {
            NarcReplace("Dat_Field.narc",    "J01_1.bin");
            NarcReplace("Dat_Basement.narc", "J02_1.bin");
            NarcReplace("Dat_Ice.narc",      "J03_1.bin");
            NarcReplace("Dat_Pipe.narc",     "J04_1.bin");
            NarcReplace("Dat_Fort.narc",     "J05_1.bin");
            NarcReplace("Dat_Field.narc",    "J01_1_bgdat.bin");
            NarcReplace("Dat_Basement.narc", "J02_1_bgdat.bin");
            NarcReplace("Dat_Ice.narc",      "J03_1_bgdat.bin");
            NarcReplace("Dat_Pipe.narc",     "J04_1_bgdat.bin");
            NarcReplace("Dat_Fort.narc",     "J05_1_bgdat.bin");

            MessageBox.Show(LanguageManager.Get("General", "Completed"));
        }

        private void MpPatch2_Click(object sender, EventArgs e)
        {
            NSMBLevel lvl = new NSMBLevel(new InternalLevelSource("J01_1", ""));
            NarcReplace("Dat_Field.narc", "I_M_nohara.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_UNT));
            NarcReplace("Dat_Field.narc", "I_M_nohara_hd.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_UNT_HD));
            NarcReplace("Dat_Field.narc", "d_2d_PA_I_M_nohara.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_PNL));
            NarcReplace("Dat_Field.narc", "NoHaRaMainUnitChangeData.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_CHK));
            NarcReplace("Dat_Field.narc", "d_2d_I_M_tikei_nohara_ncg.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_NCG));
            NarcReplace("Dat_Field.narc", "d_2d_I_M_tikei_nohara_ncl.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_NCL));

            NarcReplace("Dat_Field.narc", "d_2d_I_M_back_nohara_VS_UR_nsc.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x6], ROM.Data.Table_BG_NSC));
            NarcReplace("Dat_Field.narc", "d_2d_I_M_back_nohara_VS_ncg.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x6], ROM.Data.Table_BG_NCG));
            NarcReplace("Dat_Field.narc", "d_2d_I_M_back_nohara_VS_ncl.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x6], ROM.Data.Table_BG_NCL));

            NarcReplace("Dat_Field.narc", "d_2d_I_M_free_nohara_VS_UR_nsc.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x12], ROM.Data.Table_FG_NSC));
            NarcReplace("Dat_Field.narc", "d_2d_I_M_free_nohara_VS_ncg.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x12], ROM.Data.Table_FG_NCG));
            NarcReplace("Dat_Field.narc", "d_2d_I_M_free_nohara_VS_ncl.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x12], ROM.Data.Table_FG_NCL));


            lvl = new NSMBLevel(new InternalLevelSource("J02_1", ""));
            NarcReplace("Dat_Basement.narc", "I_M_chika3.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_UNT));
            NarcReplace("Dat_Basement.narc", "I_M_chika3_hd.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_UNT_HD));
            NarcReplace("Dat_Basement.narc", "d_2d_PA_I_M_chika3.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_PNL));
            NarcReplace("Dat_Basement.narc", "ChiKa3MainUnitChangeData.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_CHK));
            NarcReplace("Dat_Basement.narc", "d_2d_I_M_tikei_chika3_ncg.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_NCG));
            NarcReplace("Dat_Basement.narc", "d_2d_I_M_tikei_chika3_ncl.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_NCL));

            NarcReplace("Dat_Basement.narc", "d_2d_I_M_back_chika3_R_nsc.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x6], ROM.Data.Table_BG_NSC));
            NarcReplace("Dat_Basement.narc", "d_2d_I_M_back_chika3_ncg.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x6], ROM.Data.Table_BG_NCG));
            NarcReplace("Dat_Basement.narc", "d_2d_I_M_back_chika3_ncl.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x6], ROM.Data.Table_BG_NCL));


            lvl = new NSMBLevel(new InternalLevelSource("J03_1", ""));
            NarcReplace("Dat_Ice.narc", "I_M_setsugen2.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_UNT));
            NarcReplace("Dat_Ice.narc", "I_M_setsugen2_hd.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_UNT_HD));
            NarcReplace("Dat_Ice.narc", "d_2d_PA_I_M_setsugen2.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_PNL));
            NarcReplace("Dat_Ice.narc", "SeTsuGen2MainUnitChangeData.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_CHK));
            NarcReplace("Dat_Ice.narc", "d_2d_I_M_tikei_setsugen2_ncg.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_NCG));
            NarcReplace("Dat_Ice.narc", "d_2d_I_M_tikei_setsugen2_ncl.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_NCL));

            NarcReplace("Dat_Ice.narc", "d_2d_I_M_back_setsugen2_UR_nsc.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x6], ROM.Data.Table_BG_NSC));
            NarcReplace("Dat_Ice.narc", "d_2d_I_M_back_setsugen2_ncg.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x6], ROM.Data.Table_BG_NCG));
            NarcReplace("Dat_Ice.narc", "d_2d_I_M_back_setsugen2_ncl.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x6], ROM.Data.Table_BG_NCL));

            NarcReplace("Dat_Ice.narc", "d_2d_I_M_free_setsugen2_UR_nsc.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x12], ROM.Data.Table_FG_NSC));
            NarcReplace("Dat_Ice.narc", "d_2d_I_M_free_setsugen2_ncg.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x12], ROM.Data.Table_FG_NCG));
            NarcReplace("Dat_Ice.narc", "d_2d_I_M_free_setsugen2_ncl.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x12], ROM.Data.Table_FG_NCL));


            lvl = new NSMBLevel(new InternalLevelSource("J04_1", ""));
            NarcReplace("Dat_Pipe.narc", "W_M_dokansoto.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_UNT));
            NarcReplace("Dat_Pipe.narc", "W_M_dokansoto_hd.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_UNT_HD));
            NarcReplace("Dat_Pipe.narc", "d_2d_PA_W_M_dokansoto.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_PNL));
            NarcReplace("Dat_Pipe.narc", "DoKaNSoToMainUnitChangeData.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_CHK));
            NarcReplace("Dat_Pipe.narc", "d_2d_W_M_tikei_dokansoto_ncg.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_NCG));
            NarcReplace("Dat_Pipe.narc", "d_2d_W_M_tikei_dokansoto_ncl.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_NCL));

            NarcReplace("Dat_Pipe.narc", "d_2d_W_M_back_dokansoto_R_nsc.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x6], ROM.Data.Table_BG_NSC));
            NarcReplace("Dat_Pipe.narc", "d_2d_W_M_back_dokansoto_ncg.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x6], ROM.Data.Table_BG_NCG));
            NarcReplace("Dat_Pipe.narc", "d_2d_W_M_back_dokansoto_ncl.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x6], ROM.Data.Table_BG_NCL));

            NarcReplace("Dat_Pipe.narc", "d_2d_W_M_free_dokansoto_R_nsc.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x12], ROM.Data.Table_FG_NSC));
            NarcReplace("Dat_Pipe.narc", "d_2d_W_M_free_dokansoto_ncg.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x12], ROM.Data.Table_FG_NCG));
            NarcReplace("Dat_Pipe.narc", "d_2d_W_M_free_dokansoto_ncl.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x12], ROM.Data.Table_FG_NCL));


            lvl = new NSMBLevel(new InternalLevelSource("J05_1", ""));
            NarcReplace("Dat_Fort.narc", "I_M_yakata.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_UNT));
            NarcReplace("Dat_Fort.narc", "I_M_yakata_hd.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_UNT_HD));
            NarcReplace("Dat_Fort.narc", "d_2d_PA_I_M_yakata.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_PNL));
            NarcReplace("Dat_Fort.narc", "YaKaTaMainUnitChangeData.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_CHK));
            NarcReplace("Dat_Fort.narc", "d_2d_I_M_tikei_yakata_ncg.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_NCG));
            NarcReplace("Dat_Fort.narc", "d_2d_I_M_tikei_yakata_ncl.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0xC], ROM.Data.Table_TS_NCL));

            NarcReplace("Dat_Fort.narc", "d_2d_I_M_back_yakata_UR_nsc.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x6], ROM.Data.Table_BG_NSC));
            NarcReplace("Dat_Fort.narc", "d_2d_I_M_back_yakata_ncg.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x6], ROM.Data.Table_BG_NCG));
            NarcReplace("Dat_Fort.narc", "d_2d_I_M_back_yakata_ncl.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x6], ROM.Data.Table_BG_NCL));


            NarcReplace("Dat_Fort.narc", "d_2d_I_M_free_yakata_UR_nsc.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x12], ROM.Data.Table_FG_NSC));
            NarcReplace("Dat_Fort.narc", "d_2d_I_M_free_yakata_ncg.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x12], ROM.Data.Table_FG_NCG));
            NarcReplace("Dat_Fort.narc", "d_2d_I_M_free_yakata_ncl.bin", ROM.GetFileIDFromTable(lvl.Blocks[0][0x12], ROM.Data.Table_FG_NCL));

            MessageBox.Show(LanguageManager.Get("General", "Completed"));
        }

        //WTF was this for?!
        //private void NarcReplace(string NarcName, string f1, string f2) { }

        private void NarcReplace(string NarcName, string f1, ushort f2)
        {
            NarcFilesystem fs = new NarcFilesystem(ROM.FS.getFileByName(NarcName));

            NSMBe5.DSFileSystem.File f = fs.getFileByName(f1);
            if (f == null)
                Console.Out.WriteLine("No File: " + NarcName + "/" + f1);
            else
            {
                f.beginEdit(this);
                f.replace(ROM.FS.getFileById(f2).getContents(), this);
                f.endEdit(this);
            }
            fs.close();            
        }

        private void NarcReplace(string NarcName, string f1)
        {
            NarcFilesystem fs = new NarcFilesystem(ROM.FS.getFileByName(NarcName));

            NSMBe5.DSFileSystem.File f = fs.getFileByName(f1);
            f.beginEdit(this);
            f.replace(ROM.FS.getFileByName(f1).getContents(), this);
            f.endEdit(this);

            fs.close();
        }

        // Code hacking tools
        private void DecompArm9Bin_Click(object sender, EventArgs e)
        {
            Arm9BinaryHandler handler = new Arm9BinaryHandler();
            handler.decompress();
            MessageBox.Show("Arm9 binary successfully decompressed", "Arm9 binary decompressing", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private CodePatcher GetCodePatcher(DirectoryInfo path)
        {
            switch (Properties.Settings.Default.CodePatchingMethod)
            {
                case CodePatcher.Method.NSMBe:
                    return new CodePatcherNSMBe(path);
                case CodePatcher.Method.Fireflower:
                    return new CodePatcherModern(path, "fireflower");
                case CodePatcher.Method.NCPatcher:
                    return new CodePatcherModern(path, "ncpatcher");
                default:
                    throw new Exception("Illegal code patching method specified");
            }
        }

        private void CompileInsert_Click(object sender, EventArgs e)
        {
            CodePatcher patcher = GetCodePatcher(ROM.romfile.Directory);

            try
            {
                patcher.Execute();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong during code patching:\n" + ex.Message, "Code patching", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CleanBuild_Click(object sender, EventArgs e)
        {
            CodePatcher patcher = GetCodePatcher(ROM.romfile.Directory);

            try
            {
                patcher.CleanBuild();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong during build cleaning:\n" + ex.Message, "Build cleaning", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        //Settings

        private void UpdateStageObjSetsButton_Click(object sender, EventArgs e)
        {
			StageObjSettings.Update();
        }
        
        //Other crap
        
        private void LevelChooser_FormClosing(object sender, FormClosingEventArgs e)
        {
            ROM.close();
            Console.Out.WriteLine(e.CloseReason.ToString());
            Properties.Settings.Default.BackupFiles = "";
            Properties.Settings.Default.Save();
        }

        private void RenameBtn_Click(object sender, EventArgs e)
        {
            if (musicList.SelectedIndex == -1)
                return;
            string newName;
            string oldName = musicList.SelectedItem.ToString();
            oldName = oldName.Substring(oldName.IndexOf(" ") + 1);
            if (textForm.ShowDialog(this, LanguageManager.Get("LevelChooser", "rnmmusic"), oldName, out newName) == DialogResult.OK)
            {
                if (newName == string.Empty)
                {
                    ROM.UserInfo.removeListItem("Music", musicList.SelectedIndex, true);
                    musicList.Items[musicList.SelectedIndex] = string.Format("{0}: {1}", musicList.SelectedIndex, LanguageManager.GetList("Music")[musicList.SelectedIndex].Split('=')[1]);
                    return;
                }
                ROM.UserInfo.setListItem("Music", musicList.SelectedIndex, string.Format("{0}={1}", musicList.SelectedIndex, newName), true);
                musicList.Items[musicList.SelectedIndex] = string.Format("{0}: {1}", musicList.SelectedIndex, newName);
            }
        }

        private void AutoBackupTime_ValueChanged(object sender, EventArgs e)
        {
            if (init)
            {
                Properties.Settings.Default.AutoBackup = chkAutoBackup.Checked ? (int)autoBackupTime.Value : 0;
                Properties.Settings.Default.Save();
            }
        }

        private void DeleteBackups_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(LanguageManager.Get("LevelChooser", "delbackup"), LanguageManager.Get("LevelChooser", "delbacktitle"), MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
            {
                String backupPath = Path.Combine(Application.StartupPath, "Backup");
                if (System.IO.Directory.Exists(backupPath))
                    System.IO.Directory.Delete(backupPath, true);
            }
        }

        private void DlpCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ROM.dlpMode = dlpCheckBox.Checked;
            Properties.Settings.Default.dlpMode = dlpCheckBox.Checked;
            Properties.Settings.Default.Save();
        }

        private void LevelChooser_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void Xdelta_export_Click(object sender, EventArgs e)
        {
            MessageBox.Show(LanguageManager.Get("Patch", "XSelectROM") + LanguageManager.Get("Patch", "XRestartAfterApplied"), LanguageManager.Get("Patch", "XExport"), MessageBoxButtons.OK, MessageBoxIcon.Information);

            OpenFileDialog cleanROM_openFileDialog = new OpenFileDialog
            {
                Title = "Please select a clean NSMB ROM file",
                Filter = "Nintendo DS ROM (*.nds)|*.nds|All files (*.*)|*.*"
            };

            if (cleanROM_openFileDialog.ShowDialog() != DialogResult.OK)
                return;

            SaveFileDialog xdelta_saveFileDialog = new SaveFileDialog
            {
                Title = "Please select where to save the XDelta patch",
                Filter = "XDelta Patch (*.xdelta)|*.xdelta|All files (*.*)|*.*"
            };

            if (xdelta_saveFileDialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                ROM.close();

                string str = " -f -s \"" + cleanROM_openFileDialog.FileName + "\" \"" + Properties.Settings.Default.ROMPath + "\" \"" + xdelta_saveFileDialog.FileName + "\"";
                Process process = new Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.FileName = "xdelta3.exe";
                process.StartInfo.Arguments = str;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardError = true;
                process.Start();
                string end = process.StandardError.ReadToEnd();
                if (end == "")
                {
                    MessageBox.Show("Patch created successfully!", "Success!");
                }
                else
                {
                    MessageBox.Show(end, "Something went wrong!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                process.WaitForExit();
            }
            catch(Exception ex)
            {
                MessageBox.Show("An unexpected exception has occured!\nDetails:\n" + ex, LanguageManager.Get("SpriteData", "ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                //Restart NSMBe
                Process process2 = new Process();
                process2.StartInfo.FileName = Application.ExecutablePath;
                process2.StartInfo.Arguments = "\"" + Properties.Settings.Default.ROMPath + "\"";
                process2.Start();
                Application.Exit();
            }
        }

        private void Xdelta_import_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("All of the ROM contents will be replaced with the XDelta patch, unlike the NSMBe patches, this one overwrites the ROM entirely!\n\nNSMBe is going to restart after the import has finished.\n\nDo you still want to contiue?", LanguageManager.Get("General", "Warning"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.No)
                return;

            MessageBox.Show(LanguageManager.Get("Patch", "XSelectROM"), LanguageManager.Get("Patch", "XImport"), MessageBoxButtons.OK, MessageBoxIcon.Information);

            OpenFileDialog cleanROM_openFileDialog = new OpenFileDialog
            {
                Title = "Please select a clean NSMB ROM file",
                Filter = "Nintendo DS ROM (*.nds)|*.nds|All files (*.*)|*.*"
            };

            if (cleanROM_openFileDialog.ShowDialog() != DialogResult.OK)
                return;

            OpenFileDialog xdelta_openFileDialog = new OpenFileDialog
            {
                Title = "Please select the XDelta patch to import",
                Filter = "XDelta Patch (*.xdelta)|*.xdelta|All files (*.*)|*.*"
            };

            if (xdelta_openFileDialog.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                ROM.close();

                string str = " -d -f -s \"" + cleanROM_openFileDialog.FileName + "\" \"" + xdelta_openFileDialog.FileName + "\" \"" + Properties.Settings.Default.ROMPath + "\"";
                Process process = new Process();
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.FileName = "xdelta3.exe";
                process.StartInfo.Arguments = str;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardError = true;
                process.Start();
                string end = process.StandardError.ReadToEnd();
                if (end == "")
                {
                    MessageBox.Show("ROM patched successfully, restarting NSMBe...");
                }
                else
                {
                    MessageBox.Show(end + "\n\nRestarting NSMBe!", "Something went wrong!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                process.WaitForExit();

                //Restart NSMBe
                Process process2 = new Process();
                process2.StartInfo.FileName = Application.ExecutablePath;
                process2.StartInfo.Arguments = "\"" + Properties.Settings.Default.ROMPath + "\"";
                process2.Start();
                Application.Exit();
            }
            catch(Exception ex)
            {
                MessageBox.Show("An unexpected exception has occured, restarting!\nDetails:\n" + ex, LanguageManager.Get("SpriteData", "ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);

                //Restart NSMBe
                Process process2 = new Process();
                process2.StartInfo.FileName = Application.ExecutablePath;
                process2.StartInfo.Arguments = "\"" + Properties.Settings.Default.ROMPath + "\"";
                process2.Start();
                Application.Exit();
            }
        }

        private void PatchMethodComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.CodePatchingMethod = patchMethodComboBox.SelectedIndex;
            Properties.Settings.Default.Save();
        }

        private void LanguagesComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selected = languagesComboBox.SelectedItem.ToString();

            if (selected == Properties.Settings.Default.LanguageFile)
            {
                return;
            }

            Properties.Settings.Default.LanguageFile = selected;
            Properties.Settings.Default.Save();

            string text = LanguageManager.Get("LevelChooser", "LangChanged");
            string caption = LanguageManager.Get("LevelChooser", "LangChangedTitle");
            MessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LinkRepo_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/TheGameratorT/NSMB-Editor");
        }

        private void LinkOgRepo_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/Dirbaio/NSMB-Editor");
        }

        private void LinkNSMBHD_Click(object sender, EventArgs e)
        {
            Process.Start("https://nsmbhd.net");
        }

        private bool IsFontInstalled(string fontName)
        {
            using (var testFont = new Font(fontName, 8))
            {
                return fontName == testFont.Name;
            }
        }

        private void SetFontBtn_Click(object sender, EventArgs e)
        {
            string fontName = fontTextBox.Text;
            if (IsFontInstalled(fontName))
            {
                Properties.Settings.Default.UIFont = fontTextBox.Text;
                Program.ApplyFontToControls(Controls);
            }
            else
            {
                MessageBox.Show("Could not find the font \"" + fontName + "\"", "Font not found", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ManagePluginsBtn_Click(object sender, EventArgs e)
		{
			PluginSelector.Open();
		}

        #region Menu Event Handlers

        private void OpenROMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenROM();
        }

        private void OpenBackupsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenBackups();
        }

        private void CloseROMToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CloseROM();
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void ConnectToNetworkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ConnectToNetwork();
        }

        #endregion

        #region ROM Management Methods

        private void OpenROM()
        {
            string path = "";

            OpenFileDialog openROMDialog = new OpenFileDialog
            {
                Filter = LanguageManager.Get("Filters", "rom")
            };
            if (Properties.Settings.Default.ROMFolder != "")
                openROMDialog.InitialDirectory = Properties.Settings.Default.ROMFolder;
            if (openROMDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                path = openROMDialog.FileName;

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

            // Add remaining backups to the ROM backup list
            for (int l = 1; l < backups.Length; l++)
                ROM.fileBackups.Add(backups[l]);

            LoadROMFromPath(path);
        }

        private void LoadROMFromPath(string path)
        {
            try
            {
                // Ensure any previously opened ROM is closed and dependent windows are closed to release file handles
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
                
                // Add to recent files
                AddToRecentFiles(path);
                // Mark the recently loaded ROM so its project card title becomes bold
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

            // Refresh tileset and background lists
            tilesetList1.RefreshList();
            backgroundList1.RefreshList();

            // Load file backups from crash
            string backupPath = Path.Combine(Application.StartupPath, "Backup");
            if (ROM.fileBackups.Count > 0) {
                foreach (string filename in ROM.fileBackups) {
                    try
                    {
                        new LevelEditor(new NSMBLevel(LevelSource.getForBackupLevel(filename, backupPath))).Show();
                    }
                    catch (Exception) { }
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

            // When loading a ROM, automatically go to the NSMB tools tab (tabPage2) if it exists
            try
            {
                if (this.tabControl1 != null && this.tabControl1.TabPages.Contains(this.tabPage2))
                    this.tabControl1.SelectedTab = this.tabPage2;
            }
            catch { }
        }

        private void CloseROM()
        {
            if (!romLoaded) return;

            // Close all ROM-dependent windows
            CloseROMDependentWindows();

            // Clear ROM data and properly close file handles
            ROM.close();
            ROM.FS = null;
            ROM.filename = "";
            romLoaded = false;

            // Clear UI
            levelTreeView.Nodes.Clear();
            musicList.Items.Clear();
            filesystemBrowser1.Load(null);

            // Refresh tileset and background lists (clear them)
            tilesetList1.RefreshList();
            backgroundList1.RefreshList();

            // Update title
            Text = "NSMB Editor " + Version.GetString();

            DisableROMDependentControls();
            UpdateMenuState();
        }

        private void CloseROMDependentWindows()
        {
            // Close static forms that are tracked
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

            // Close all forms that depend on ROM data
            Form[] openForms = Application.OpenForms.Cast<Form>().ToArray();
            foreach (Form form in openForms)
            {
                // Skip the main LevelChooser form
                if (form == this)
                    continue;

                // Close forms
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
            // Re-enable the whole tab control and all tab pages when a ROM is loaded
            tabControl1.Enabled = true;
            foreach (System.Windows.Forms.TabPage tp in tabControl1.TabPages)
            {
                tp.Enabled = true;
            }
        }

        private void DisableROMDependentControls()
        {
            // Keep tabControl enabled so Projects tab remains usable even without a ROM
            tabControl1.Enabled = true;
            // Disable only ROM-dependent tab pages. Projects (tabPage0) and About (tabPage4) stay enabled.
            foreach (System.Windows.Forms.TabPage tp in tabControl1.TabPages)
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
            recentFilesToolStripMenuItem.Enabled = true; // Recent files should always be accessible
        }

        #endregion

        #region Recent Files Management

        private const int MaxRecentFiles = 10;

        private void LoadRecentFiles()
        {
            UpdateRecentFilesMenu();
        }

        private void AddToRecentFiles(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            var recentFiles = GetRecentFiles();
            
            // Remove if already exists
            recentFiles.Remove(filePath);
            
            // Add to beginning
            recentFiles.Insert(0, filePath);
            
            // Keep only MaxRecentFiles
            while (recentFiles.Count > MaxRecentFiles)
                recentFiles.RemoveAt(recentFiles.Count - 1);
            
            // Save back to settings
            Properties.Settings.Default.RecentFiles = string.Join(";", recentFiles.ToArray());
            Properties.Settings.Default.Save();
            
            UpdateRecentFilesMenu();
        }

        private List<string> GetRecentFiles()
        {
            var recentFiles = new List<string>();
            if (!string.IsNullOrEmpty(Properties.Settings.Default.RecentFiles))
            {
                var files = Properties.Settings.Default.RecentFiles.Split(';');
                foreach (var file in files)
                {
                    if (!string.IsNullOrEmpty(file) && System.IO.File.Exists(file))
                        recentFiles.Add(file);
                }
            }
            return recentFiles;
        }

        private void UpdateRecentFilesMenu()
        {
            // Clear existing recent files
            recentFilesToolStripMenuItem.DropDownItems.Clear();
            
            var recentFiles = GetRecentFiles();
            
            if (recentFiles.Count == 0)
            {
                var emptyItem = new ToolStripMenuItem("(No recent files)")
                {
                    Enabled = false
                };
                recentFilesToolStripMenuItem.DropDownItems.Add(emptyItem);
            }
            else
            {
                for (int i = 0; i < recentFiles.Count; i++)
                {
                    var fileName = Path.GetFileName(recentFiles[i]);
                    var menuText = $"&{i + 1} {fileName}";
                    var menuItem = new ToolStripMenuItem(menuText)
                    {
                        Tag = recentFiles[i]
                    };
                    menuItem.Click += RecentFileMenuItem_Click;
                    recentFilesToolStripMenuItem.DropDownItems.Add(menuItem);
                }
                
                // Add separator and clear option
                recentFilesToolStripMenuItem.DropDownItems.Add(new ToolStripSeparator());
                var clearItem = new ToolStripMenuItem("Clear Recent Files");
                clearItem.Click += ClearRecentFiles_Click;
                recentFilesToolStripMenuItem.DropDownItems.Add(clearItem);
            }

            UpdateRecentFilesPanel();
        }

        private void RecentFileMenuItem_Click(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            if (menuItem?.Tag is string filePath)
            {
                if (System.IO.File.Exists(filePath))
                {
                    LoadROMFromPath(filePath);
                    MarkRecentOpened(filePath);
                    UpdateCardModifiedLabel(filePath);
                }
                else
                {
                    MessageBox.Show($"File not found: {filePath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    // Remove from recent files if it doesn't exist
                    var recentFiles = GetRecentFiles();
                    recentFiles.Remove(filePath);
                    Properties.Settings.Default.RecentFiles = string.Join(";", recentFiles.ToArray());
                    Properties.Settings.Default.Save();
                    UpdateRecentFilesMenu();
                }
            }
        }

        private void ClearRecentFiles_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.RecentFiles = "";
            Properties.Settings.Default.Save();
            UpdateRecentFilesMenu();
        }

        private void UpdateRecentFilesPanel()
        {
            try
            {
                if (this.projectsPanel == null) return;
                EnsureProjectsPanelNoHorizontalScroll();

                this.projectsPanel.Controls.Clear();
                var recentFiles = GetRecentFiles();

                if (recentFiles.Count == 0)
                {
                    var lbl = new Label() { Text = "(No recent projects)", AutoSize = true, ForeColor = SystemColors.GrayText };
                    this.projectsPanel.Controls.Add(lbl);
                }
                else
                {
                    string filter = "";
                    if (this.searchBox != null)
                    {
                        // If placeholder is showing (gray text), treat as empty filter to display all recent files
                        if (this.searchBox.ForeColor == SystemColors.GrayText)
                            filter = "";
                        else
                            filter = this.searchBox.Text?.ToLowerInvariant() ?? "";
                    }
                    foreach (var f in recentFiles)
                    {
                        if (!string.IsNullOrEmpty(filter))
                        {
                            var name = Path.GetFileNameWithoutExtension(f).ToLowerInvariant();
                            if (!f.ToLowerInvariant().Contains(filter) && !name.Contains(filter))
                                continue;
                        }
                        var card = CreateProjectCard(f);
                        this.projectsPanel.Controls.Add(card);
                        this.projectsPanel.SetFlowBreak(card, true);
                    }
                }
                EnsureProjectsPanelNoHorizontalScroll();
            }
            catch { }
        }

        private int GetProjectsCardWidth()
        {
            if (this.projectsPanel == null) return 440;
            int safety = 2;
            int reservedForVScroll = SystemInformation.VerticalScrollBarWidth;
            int width = this.projectsPanel.ClientSize.Width - reservedForVScroll - safety;
            return Math.Max(240, width);
        }

        private void EnsureProjectsPanelNoHorizontalScroll()
        {
            if (this.projectsPanel == null) return;
            try
            {
                this.projectsPanel.HorizontalScroll.Maximum = 0;
                this.projectsPanel.HorizontalScroll.Visible = false;
                this.projectsPanel.HorizontalScroll.Enabled = false;

                if (this.projectsPanel.IsHandleCreated)
                {
                    ShowScrollBar(this.projectsPanel.Handle, SB_HORZ, false);
                }
            }
            catch { }
        }

        // Persistence for custom project display names
        private string GetProjectDisplayNamesFilePath()
        {
            return Path.Combine(Application.StartupPath, "project_display_names.txt");
        }

        private void LoadProjectDisplayNames()
        {
            projectDisplayNames.Clear();
            try
            {
                string fn = GetProjectDisplayNamesFilePath();
                if (!System.IO.File.Exists(fn)) return;
                foreach (var line in System.IO.File.ReadAllLines(fn))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    int idx = line.IndexOf('\t');
                    if (idx <= 0) continue;
                    string p = line.Substring(0, idx);
                    string n = line.Substring(idx + 1);
                    projectDisplayNames[p] = n;
                }
            }
            catch { }
        }

        private void SaveProjectDisplayNames()
        {
            try
            {
                string fn = GetProjectDisplayNamesFilePath();
                var lines = new List<string>();
                foreach (var kv in projectDisplayNames)
                {
                    string safeName = kv.Value?.Replace('\r', ' ').Replace('\n', ' ')
                                               .Replace('\t', ' ') ?? "";
                    lines.Add(kv.Key + "\t" + safeName);
                }
                System.IO.File.WriteAllLines(fn, lines.ToArray());
            }
            catch { }
        }

        private string GetProjectDisplayName(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return string.Empty;
            if (projectDisplayNames.TryGetValue(filePath, out var name) && !string.IsNullOrEmpty(name))
                return name;
            return Path.GetFileNameWithoutExtension(filePath);
        }

        private void SetProjectDisplayName(string filePath, string displayName)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            if (string.IsNullOrEmpty(displayName))
            {
                if (projectDisplayNames.ContainsKey(filePath))
                    projectDisplayNames.Remove(filePath);
            }
            else
            {
                projectDisplayNames[filePath] = displayName;
            }
            SaveProjectDisplayNames();
        }

        private Panel CreateProjectCard(string filePath)
        {
            int cardWidth = GetProjectsCardWidth();
            var panel = new Panel
            {
                Width = cardWidth,
                Height = 36,
                Margin = new Padding(8),
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                MinimumSize = new Size(240, 36)
            };

            var nameLbl = new Label
            {
                AutoSize = false,
                Location = new Point(8, 4),
                Size = new Size(panel.Width - 180, 16),
                AutoEllipsis = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Text = GetProjectDisplayName(filePath)
            };
            nameLbl.Tag = "nameLbl";
            // If this project is the currently loaded recent file, show title bold
            if (!string.IsNullOrEmpty(currentLoadedRecentFile) && string.Equals(currentLoadedRecentFile, filePath, StringComparison.OrdinalIgnoreCase))
            {
                try { nameLbl.Font = new Font(nameLbl.Font, FontStyle.Bold); } catch { }
            }
            panel.Controls.Add(nameLbl);

            var pathLbl = new Label
            {
                AutoSize = false,
                Location = new Point(8, 20),
                Size = new Size(panel.Width - 180, 12),
                AutoEllipsis = true,
                Font = new Font("Segoe UI", 8F),
                ForeColor = SystemColors.GrayText,
                Text = filePath
            };
            panel.Controls.Add(pathLbl);

            var modifiedLbl = new Label
            {
                AutoSize = false,
                Location = new Point(panel.Width - 120, 6),
                Size = new Size(110, 24),
                Font = new Font("Segoe UI", 8F),
                ForeColor = SystemColors.GrayText,
                TextAlign = ContentAlignment.MiddleRight,
                Text = GetRelativeTimeString(System.IO.File.GetLastWriteTime(filePath)),
                Tag = "modifiedLbl"
            };
            panel.Controls.Add(modifiedLbl);

            // Tooltips for truncated text
            try
            {
                if (this.toolTip1 != null)
                {
                    this.toolTip1.SetToolTip(pathLbl, filePath);
                    this.toolTip1.SetToolTip(modifiedLbl, System.IO.File.GetLastWriteTime(filePath).ToString());
                }
            }
            catch { }

            panel.Tag = filePath;
            panel.Click += (s, e) => { /* select on click if needed */ };
            nameLbl.Click += (s, e) => { /* select on click if needed */ };
            pathLbl.Click += (s, e) => { /* select on click if needed */ };
            panel.DoubleClick += (s, e) => {
                if (System.IO.File.Exists(filePath)) {
                    LoadROMFromPath(filePath);
                    MarkRecentOpened(filePath);
                    UpdateCardModifiedLabel(filePath);
                }
            };
            // Ensure double-click on child labels also opens the project
            nameLbl.DoubleClick += (s, e) => {
                if (System.IO.File.Exists(filePath)) {
                    LoadROMFromPath(filePath);
                    MarkRecentOpened(filePath);
                    UpdateCardModifiedLabel(filePath);
                }
            };
            pathLbl.DoubleClick += (s, e) => {
                if (System.IO.File.Exists(filePath)) {
                    LoadROMFromPath(filePath);
                    MarkRecentOpened(filePath);
                    UpdateCardModifiedLabel(filePath);
                }
            };

            // context menu for this card
            var ctx = new ContextMenuStrip();
            var showItem = new ToolStripMenuItem(LanguageManager.Get("LevelChooser", "showInExplorerToolStripMenuItem") ?? "Show in Explorer") { Tag = filePath };
            showItem.Click += (s, e) => { ShowInExplorerFor(filePath); };
            var setNameItem = new ToolStripMenuItem(LanguageManager.Get("LevelChooser", "setProjectDisplayNameToolStripMenuItem") ?? "Set project display name") { Tag = filePath };
            setNameItem.Click += (s, e) =>
            {
                try
                {
                    string current = GetProjectDisplayName(filePath);
                    if (textForm.ShowDialog(this, LanguageManager.Get("LevelChooser", "SetProjectDisplayName") ?? "Set project display name", current, out string newName) == DialogResult.OK)
                    {
                        SetProjectDisplayName(filePath, newName);
                        nameLbl.Text = GetProjectDisplayName(filePath);
                    }
                }
                catch { }
            };
            var removeItem = new ToolStripMenuItem(LanguageManager.Get("LevelChooser", "removeProjectToolStripMenuItem") ?? "Remove project from list") { Tag = filePath };
            removeItem.Click += (s, e) => { RemoveProjectFor(filePath); };
            ctx.Items.Add(showItem);
            ctx.Items.Add(setNameItem);
            ctx.Items.Add(removeItem);
            panel.ContextMenuStrip = ctx;

            return panel;
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            // If the placeholder is showing, don't trigger search.
            if (this.searchBox.ForeColor == SystemColors.GrayText)
                return;

            // Only start filtering once the user has entered text (first character).
            UpdateRecentFilesPanel();
        }

        

        private void SearchBox_Enter(object sender, EventArgs e)
        {
            if (this.searchBox.ForeColor == SystemColors.GrayText)
            {
                this.searchBox.Text = string.Empty;
                this.searchBox.ForeColor = SystemColors.WindowText;
            }
        }

        private void SearchBox_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.searchBox.Text))
            {
                this.searchBox.ForeColor = SystemColors.GrayText;
                this.searchBox.Text = "Search";
            }
        }

        private void AddProjectButton_Click(object sender, EventArgs e)
        {
            if (openROMDialog.ShowDialog() == DialogResult.OK)
            {
                var path = openROMDialog.FileName;

                // If the project is already in recent files, open it immediately
                var recentFiles = GetRecentFiles();
                if (recentFiles.Any(f => string.Equals(f, path, StringComparison.OrdinalIgnoreCase)))
                {
                    if (System.IO.File.Exists(path))
                    {
                        LoadROMFromPath(path);
                        MarkRecentOpened(path);
                        UpdateCardModifiedLabel(path);
                    }
                    return;
                }

                bool shouldAdd = false;
                try
                {
                    string defaultName = Path.GetFileNameWithoutExtension(path);
                    var dr = textForm.ShowDialog(this, LanguageManager.Get("LevelChooser", "SetProjectDisplayName") ?? "Set project display name", defaultName, out string newName);
                    if (dr == DialogResult.OK)
                    {
                        shouldAdd = true;
                        if (!string.IsNullOrEmpty(newName))
                            projectDisplayNames[path] = newName;
                        else if (projectDisplayNames.ContainsKey(path))
                            projectDisplayNames.Remove(path);
                        SaveProjectDisplayNames();
                    }
                }
                catch { }

                if (shouldAdd)
                {
                    AddToRecentFiles(path);
                    if (System.IO.File.Exists(path))
                    {
                        LoadROMFromPath(path);
                        MarkRecentOpened(path);
                        UpdateCardModifiedLabel(path);
                    }
                }
            }
        }

        private void RecentFilesListBox_DoubleClick(object sender, EventArgs e)
        {
            if (recentFilesListBox == null) return;
            if (recentFilesListBox.SelectedItem is string filePath)
            {
                if (System.IO.File.Exists(filePath))
                {
                    LoadROMFromPath(filePath);
                    MarkRecentOpened(filePath);
                    UpdateCardModifiedLabel(filePath);
                }
                else
                {
                    MessageBox.Show($"File not found: {filePath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    var recentFiles = GetRecentFiles();
                    recentFiles.Remove(filePath);
                    Properties.Settings.Default.RecentFiles = string.Join(";", recentFiles.ToArray());
                    Properties.Settings.Default.Save();
                    UpdateRecentFilesMenu();
                }
            }
        }

        private void RecentFilesListBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (recentFilesListBox == null) return;
            if (e.Button == MouseButtons.Right)
            {
                int idx = recentFilesListBox.IndexFromPoint(e.Location);
                if (idx >= 0 && idx < recentFilesListBox.Items.Count)
                    recentFilesListBox.SelectedIndex = idx;
                else
                    recentFilesListBox.ClearSelected();
            }
        }

        private void ProjectsPanel_SizeChanged(object sender, EventArgs e)
        {
            if (this.projectsPanel == null) return;
            try
            {
                // Adjust searchBox width to be responsive relative to the panel width
                if (this.searchBox != null && this.addProjectButton != null)
                {
                    int availableWidth = this.projectsPanel.Width - this.addProjectButton.Width - 32;
                    this.searchBox.Width = Math.Max(100, availableWidth);
                }

                foreach (Control c in this.projectsPanel.Controls)
                {
                    if (c is Panel panel)
                    {
                        this.projectsPanel.SetFlowBreak(panel, true);
                        int newWidth = GetProjectsCardWidth();
                        panel.Width = newWidth;
                        // adjust child controls positions/sizes
                        foreach (Control child in panel.Controls)
                        {
                            if (child is Label lbl)
                            {
                                if (lbl.Tag is string t && t == "modifiedLbl")
                                {
                                    lbl.Location = new Point(panel.Width - 120, lbl.Location.Y);
                                    lbl.Width = 110;
                                }
                                else
                                {
                                    lbl.Width = panel.Width - 180;
                                }
                            }
                            else if (child is Button btn)
                            {
                                if (btn.Text == "Open")
                                    btn.Location = new Point(panel.Width - 140, btn.Location.Y);
                                else if (btn.Text == "Remove")
                                    btn.Location = new Point(panel.Width - 80, btn.Location.Y);
                            }
                        }
                    }
                }
                EnsureProjectsPanelNoHorizontalScroll();
            }
            catch { }
        }

        private void RecentFilesContextMenu_Opening(object sender, CancelEventArgs e)
        {
            if (recentFilesListBox == null) { e.Cancel = true; return; }
            if (recentFilesListBox.SelectedItem is string filePath)
            {
                // enable items only when an existing file is selected
                bool exists = System.IO.File.Exists(filePath);
                showInExplorerToolStripMenuItem.Enabled = exists;
                removeProjectToolStripMenuItem.Enabled = true;
            }
            else
            {
                // cancel opening if nothing selected
                e.Cancel = true;
            }
        }

        private void ShowInExplorerMenuItem_Click(object sender, EventArgs e)
        {
            if (recentFilesListBox == null) return;
            if (recentFilesListBox.SelectedItem is string filePath)
            {
                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        Process.Start("explorer.exe", "/select,\"" + filePath + "\"");
                    }
                    catch
                    {
                        try { Process.Start("explorer.exe", Path.GetDirectoryName(filePath)); } catch { }
                    }
                }
                else
                {
                    MessageBox.Show($"File not found: {filePath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    var recentFiles = GetRecentFiles();
                    recentFiles.Remove(filePath);
                    Properties.Settings.Default.RecentFiles = string.Join(";", recentFiles.ToArray());
                    Properties.Settings.Default.Save();
                    UpdateRecentFilesMenu();
                }
            }
        }

        private void ShowInExplorerFor(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    Process.Start("explorer.exe", "/select,\"" + filePath + "\"");
                }
                catch
                {
                    try { Process.Start("explorer.exe", Path.GetDirectoryName(filePath)); } catch { }
                }
            }
            else
            {
                MessageBox.Show($"File not found: {filePath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                var recentFiles = GetRecentFiles();
                recentFiles.Remove(filePath);
                Properties.Settings.Default.RecentFiles = string.Join(";", recentFiles.ToArray());
                Properties.Settings.Default.Save();
                UpdateRecentFilesMenu();
            }
        }

        private void RemoveProjectFor(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            var recentFiles = GetRecentFiles();
            recentFiles.Remove(filePath);
            Properties.Settings.Default.RecentFiles = string.Join(";", recentFiles.ToArray());
            Properties.Settings.Default.Save();
            // If the removed project was the currently loaded one, clear bold state
            if (!string.IsNullOrEmpty(currentLoadedRecentFile) && string.Equals(currentLoadedRecentFile, filePath, StringComparison.OrdinalIgnoreCase))
            {
                currentLoadedRecentFile = null;
            }
            // also remove any custom display name and persist
            if (projectDisplayNames.ContainsKey(filePath))
            {
                projectDisplayNames.Remove(filePath);
                SaveProjectDisplayNames();
            }
            UpdateRecentFilesMenu();
            UpdateRecentFilesPanel();
        }

        private void RemoveProjectMenuItem_Click(object sender, EventArgs e)
        {
            if (recentFilesListBox == null) return;
            if (recentFilesListBox.SelectedItem is string filePath)
            {
                RemoveProjectFor(filePath);
            }
        }

        #endregion
    }
}
