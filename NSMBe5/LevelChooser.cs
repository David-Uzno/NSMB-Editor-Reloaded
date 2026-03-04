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
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using NSMBe5.Editor;

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

        private DataFinder DataFinderForm;

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

            romLoaded = ROM.FS != null;

            if (romLoaded)
                LoadROMDependentData();
            else
            {
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

            LoadProjectDisplayNames();
            LoadRecentFiles();

            string langDir = Path.Combine(Application.StartupPath, "Languages");
            if (Directory.Exists(langDir))
            {
                string[] files = Directory.GetFiles(langDir);
                for (int l = 0; l < files.Length; l++)
                {
                    if (files[l].EndsWith(".ini"))
                    {
                        int startPos = files[l].LastIndexOf(Path.DirectorySeparatorChar) + 1;
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

            InitializeModernLevelListingUI();
            InitializePreviewModeOptionUI();
            ApplyModernWhiteTheme();
            ApplyLevelListingPreviewMode(false);
            if (levelTreeView != null)
                levelTreeView.LineColor = System.Drawing.Color.FromArgb(210, 210, 210);

            UpdateMenuState();
            Activate();
        }
    }
}
