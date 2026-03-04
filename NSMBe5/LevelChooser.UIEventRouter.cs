using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace NSMBe5
{
    public partial class LevelChooser
    {
        private void LevelTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            bool enabled = e.Node.Tag != null;
            importLevelButton.Enabled = enabled;
            exportLevelButton.Enabled = enabled;
            editLevelButton.Enabled = enabled;
            hexEditLevelButton.Enabled = enabled;
            importClipboard.Enabled = enabled;
            exportClipboard.Enabled = enabled;

            if (enabled)
                UpdateLevelPreview(e.Node.Tag as string);
            else
                UpdateLevelPreview(null);
        }

        private void EditLevelButton_Click(object sender, EventArgs e)
        {
            EditSelectedLevel();
        }

        private void HexEditLevelButton_Click(object sender, EventArgs e)
        {
            HexEditSelectedLevel();
        }

        private void LevelTreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag != null && editLevelButton.Enabled)
                EditSelectedLevel();
        }

        private void ImportLevelButton_Click(object sender, EventArgs e)
        {
            ImportSelectedLevel();
        }

        private void ExportLevelButton_Click(object sender, EventArgs e)
        {
            ExportSelectedLevel();
        }

        private void OpenLevel_Click(object sender, EventArgs e)
        {
            OpenExternalLevel();
        }

        private void ImportClipboard_Click(object sender, EventArgs e)
        {
            ImportLevelFromClipboard();
        }

        private void ExportClipboard_Click(object sender, EventArgs e)
        {
            ExportLevelToClipboard();
        }

        private void OpenClipboard_Click(object sender, EventArgs e)
        {
            OpenLevelFromClipboard();
        }

        private void DataFinderButton_Click(object sender, EventArgs e)
        {
            ShowDataFinder();
        }

        private void UpdateStageObjSetsButton_Click(object sender, EventArgs e)
        {
            StageObjSettings.Update();
        }

        private void LevelChooser_FormClosing(object sender, FormClosingEventArgs e)
        {
            ROM.close();
            Console.Out.WriteLine(e.CloseReason.ToString());
            Properties.Settings.Default.BackupFiles = "";
            Properties.Settings.Default.Save();
        }

        private void LevelChooser_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
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
            if (MessageBox.Show(LanguageManager.Get("LevelChooser", "delbackup"), LanguageManager.Get("LevelChooser", "delbacktitle"), MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                string backupPath = Path.Combine(Application.StartupPath, "Backup");
                if (Directory.Exists(backupPath))
                    Directory.Delete(backupPath, true);
            }
        }

        private void DlpCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ROM.dlpMode = dlpCheckBox.Checked;
            Properties.Settings.Default.dlpMode = dlpCheckBox.Checked;
            Properties.Settings.Default.Save();
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
                return;

            Properties.Settings.Default.LanguageFile = selected;
            Properties.Settings.Default.Save();

            string text = LanguageManager.Get("LevelChooser", "LangChanged");
            string caption = LanguageManager.Get("LevelChooser", "LangChangedTitle");
            MessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LinkRepo_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/TheGameratorT/NSMB-Editor");
        }

        private void LinkOgRepo_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/Dirbaio/NSMB-Editor");
        }

        private void LinkNSMBHD_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://nsmbhd.net");
        }

        private bool IsFontInstalled(string fontName)
        {
            using (var testFont = new Font(fontName, 8))
                return fontName == testFont.Name;
        }

        private void SetFontBtn_Click(object sender, EventArgs e)
        {
            string fontName = fontTextBox.Text;
            if (IsFontInstalled(fontName))
            {
                Properties.Settings.Default.UIFont = fontTextBox.Text;
                Program.ApplyFontToControls(Controls);
                ApplyModernWhiteTheme();
            }
            else
            {
                MessageBox.Show("Could not find the font \"" + fontName + "\"", "Font not found", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ManagePluginsBtn_Click(object sender, EventArgs e)
        {
            OpenPluginManager();
        }

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
    }
}
