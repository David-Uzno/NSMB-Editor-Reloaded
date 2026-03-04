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
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using NSMBe5.DSFileSystem;
using WeifenLuo.WinFormsUI.Docking;
using WeifenLuo.WinFormsUI.ThemeVS2015;


namespace NSMBe5 {
    public partial class LevelEditor : Form {

        public ObjectsEditionMode oem;

        public BackgroundDragEditionMode bgdragem;

        public ToolsForm tools;
        public StageObjEventsViewer sprEvents;

        public LevelEditor(NSMBLevel Level)
        {
            InitializeComponent();

            this.Level = Level;
            this.GFX = Level.GFX;
            coordinateViewer1.EdControl = levelEditorControl1;
            //This is supposed to reduce flickering on stuff like the side panel...
            //But it doesn't :(
            this.SetStyle(
              ControlStyles.AllPaintingInWmPaint|
              ControlStyles.UserPaint |
              ControlStyles.DoubleBuffer, true); 
            
            if (Properties.Settings.Default.LevelMaximized)
                this.WindowState = FormWindowState.Maximized;

            smallBlockOverlaysToolStripMenuItem.Checked = Properties.Settings.Default.SmallBlockOverlays;
            showResizeHandles.Checked = Properties.Settings.Default.ShowResizeHandles;

            LanguageManager.ApplyToContainer(this, "LevelEditor");
            this.Text = LanguageManager.Get("General", "EditingSomething") + " " + Level.name;
            // these need to be added manually
            reloadTilesets.Text = LanguageManager.Get("LevelEditor", "reloadTilesets");
            smallBlockOverlaysToolStripMenuItem.Text = LanguageManager.Get("LevelEditor", "smallBlockOverlaysToolStripMenuItem");
            showResizeHandles.Text = LanguageManager.Get("LevelEditor", "showResizeHandles");
            setBgImageButton.Text = LanguageManager.Get("LevelEditor", "setBgImageButton");
            removeBgButton.Text = LanguageManager.Get("LevelEditor", "removeBgButton");
            moveBGToolStripMenuItem.Text = LanguageManager.Get("LevelEditor", "moveBGToolStripMenuItem");
            openImage.Filter = LanguageManager.Get("Filters", "image");

            levelEditorControl1.LoadUndoManager(undoButton, redoButton);

            Level.enableWrite();
            levelEditorControl1.Initialise(GFX, Level, this);

            oem = new ObjectsEditionMode(Level, levelEditorControl1);
            bgdragem = new BackgroundDragEditionMode(Level, levelEditorControl1);

            levelEditorControl1.SetEditionMode(oem);
            levelEditorControl1.minimapctrl = minimapControl1;

            tools = new ToolsForm(levelEditorControl1);
            sprEvents = new StageObjEventsViewer(levelEditorControl1);
            MinimapForm = new LevelMinimap(Level, levelEditorControl1);
            levelEditorControl1.minimap = MinimapForm;
            MinimapForm.Text = string.Format(LanguageManager.Get("LevelEditor", "MinimapTitle"), Level.name);
            minimapControl1.loadMinimap(Level, levelEditorControl1);
            this.Icon = Properties.Resources.nsmbe;

            if (Properties.Settings.Default.AutoBackup > 0)
            {
                backupTimer.Interval = Properties.Settings.Default.AutoBackup * 60000;
                backupTimer.Start();
			}

			Program.ApplyFontToControls(Controls);

            InitializeDockingWorkspace();
            InitializeLayoutPresetSelector();
		}

        private void reloadTilesets_Click(object sender, EventArgs e) {
            byte TilesetID = Level.Blocks[0][0x0C];
            byte BGNSCID = Level.Blocks[2][2];
            LevelConfigForm_ReloadTileset();
        }

        private LevelMinimap MinimapForm;
        private LevelConfig LevelConfigForm;
        private NSMBLevel Level;
        private NSMBGraphics GFX;
        private DockPanel editingDockPanel;
        private DockableEditorWindow editorDockWindow;
        private DockableEditorWindow panelDockWindow;
        private DockableEditorWindow minimapDockWindow;
        private DockableEditorWindow coordinateDockWindow;
        private readonly Dictionary<string, DockContent> dockContents = new Dictionary<string, DockContent>();
        private ToolStripComboBox layoutPresetCombo;
        private ToolStripLabel layoutPresetLabel;
        private bool ignoreLayoutPresetChanges;

        private const string EditorDockKey = "Editing.Editor";
        private const string PanelDockKey = "Editing.Panel";
        private const string MinimapDockKey = "Editing.Minimap";
        private const string CoordinateDockKey = "Editing.Coordinates";
        private const string LayoutPresetDefault = "default";
        private const string LayoutPresetClassic = "classic";
        private const string LayoutPresetVertical = "vertical";

        public Bitmap CaptureFullLevelPreview(int width, int height)
        {
            if (levelEditorControl1 == null)
                return null;

            try
            {
                levelEditorControl1.updateTileCache(true);
                levelEditorControl1.repaint();
                this.Update();
                levelEditorControl1.Update();
                for (int i = 0; i < 3; i++)
                    Application.DoEvents();
                return levelEditorControl1.CreateFullLevelPreview(width, height);
            }
            catch
            {
                return null;
            }
        }

        private UserControl SelectedPanel;

        private void InitializeDockingWorkspace()
        {
            editingDockPanel = new DockPanel
            {
                Dock = DockStyle.Fill,
                DocumentStyle = DocumentStyle.DockingWindow,
                Name = "editingDockPanel"
            };
            editingDockPanel.Theme = new VS2015LightTheme();
            ConfigureDockHeaderAppearance();

            Controls.Add(editingDockPanel);

            if (editingDockPanel.Extender != null)
                editingDockPanel.Extender.FloatWindowFactory = new NonClosableFloatWindowFactory();

            toolStrip1.BringToFront();

            if (splitContainer1 != null)
            {
                splitContainer1.Visible = false;
                if (splitContainer1.Parent != null)
                    splitContainer1.Parent.Controls.Remove(splitContainer1);
            }

            editorDockWindow = new DockableEditorWindow(EditorDockKey, "Editor", levelEditorControl1, editingDockPanel, DockState.Document);
            panelDockWindow = new DockableEditorWindow(PanelDockKey, "Panel", PanelContainer, editingDockPanel, DockState.DockLeft);
            minimapDockWindow = new DockableEditorWindow(MinimapDockKey, string.Format(LanguageManager.Get("LevelEditor", "MinimapTitle"), Level.name), minimapControl1, editingDockPanel, DockState.DockBottom);
            coordinateDockWindow = new DockableEditorWindow(CoordinateDockKey, "Coordinates", coordinateViewer1, editingDockPanel, DockState.DockBottom);

            dockContents.Clear();
            dockContents[EditorDockKey] = editorDockWindow;
            dockContents[PanelDockKey] = panelDockWindow;
            dockContents[MinimapDockKey] = minimapDockWindow;
            dockContents[CoordinateDockKey] = coordinateDockWindow;

            foreach (DockContent content in dockContents.Values)
                content.DockStateChanged += DockContent_DockStateChanged;

            ApplyDefaultDockLayout();
        }

        private void ConfigureDockHeaderAppearance()
        {
            if (editingDockPanel == null || editingDockPanel.Theme == null)
                return;

            if (editingDockPanel.Theme.Measures != null)
                editingDockPanel.Theme.Measures.DockPadding = Math.Max(editingDockPanel.Theme.Measures.DockPadding, 12);

            DockPanelSkin skin = editingDockPanel.Skin ?? editingDockPanel.Theme.Skin;
            if (skin != null && skin.DockPaneStripSkin != null)
            {
                skin.DockPaneStripSkin.TextFont = SystemFonts.MessageBoxFont;

                DockPaneStripToolWindowGradient toolGradient = skin.DockPaneStripSkin.ToolWindowGradient;
                if (toolGradient != null)
                {
                    CopyTabGradient(toolGradient.InactiveCaptionGradient, toolGradient.ActiveCaptionGradient);
                    CopyTabGradient(toolGradient.InactiveTabGradient, toolGradient.ActiveTabGradient);
                    CopyTabGradient(toolGradient.InactiveTabGradient, toolGradient.HoverTabGradient);
                }

                DockPaneStripGradient documentGradient = skin.DockPaneStripSkin.DocumentGradient;
                if (documentGradient != null)
                {
                    CopyTabGradient(documentGradient.InactiveTabGradient, documentGradient.ActiveTabGradient);
                    CopyTabGradient(documentGradient.InactiveTabGradient, documentGradient.HoverTabGradient);
                }
            }
        }

        private static void CopyTabGradient(TabGradient source, TabGradient target)
        {
            if (source == null || target == null)
                return;

            target.StartColor = source.StartColor;
            target.EndColor = source.EndColor;
            target.TextColor = source.TextColor;
            target.LinearGradientMode = source.LinearGradientMode;
        }

        private void InitializeLayoutPresetSelector()
        {
            layoutPresetCombo = new ToolStripComboBox
            {
                Name = "layoutPresetCombo",
                Alignment = ToolStripItemAlignment.Right,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 190
            };

            layoutPresetLabel = new ToolStripLabel
            {
                Name = "layoutPresetLabel",
                Alignment = ToolStripItemAlignment.Right,
                Text = "Layout"
            };

            layoutPresetCombo.Items.Add(new LayoutPresetItem(LayoutPresetDefault, "Por defecto"));
            layoutPresetCombo.Items.Add(new LayoutPresetItem(LayoutPresetClassic, "Clásico"));
            layoutPresetCombo.Items.Add(new LayoutPresetItem(LayoutPresetVertical, "Columnas"));
            layoutPresetCombo.SelectedIndexChanged += LayoutPresetCombo_SelectedIndexChanged;

            toolStrip1.Items.Add(layoutPresetCombo);
            toolStrip1.Items.Add(layoutPresetLabel);

            ignoreLayoutPresetChanges = true;
            layoutPresetCombo.SelectedIndex = 0;
            ignoreLayoutPresetChanges = false;
        }

        private void ApplyDefaultDockLayout()
        {
            HideAllDockWindows();
            editorDockWindow.Show(editingDockPanel, DockState.Document);
            panelDockWindow.Show(editorDockWindow.Pane, DockAlignment.Left, 0.25);
            minimapDockWindow.Show(editingDockPanel, DockState.DockBottomAutoHide);
            coordinateDockWindow.Show(editingDockPanel, DockState.DockBottomAutoHide);
        }

        private void ApplyClassicDockLayout()
        {
            HideAllDockWindows();
            editorDockWindow.Show(editingDockPanel, DockState.Document);
            panelDockWindow.Show(editingDockPanel, DockState.DockLeft);
            minimapDockWindow.Show(editingDockPanel, DockState.DockBottom);
            coordinateDockWindow.Show(minimapDockWindow.Pane, DockAlignment.Right, 0.5);
        }

        private void ApplyVerticalLayout()
        {
            HideAllDockWindows();
            editorDockWindow.Show(editingDockPanel, DockState.Document);
            panelDockWindow.Show(editingDockPanel, DockState.DockLeft);
            minimapDockWindow.Show(editorDockWindow.Pane, DockAlignment.Right, 0.35);
            coordinateDockWindow.Show(minimapDockWindow.Pane, DockAlignment.Bottom, 0.5);
        }

        private void HideAllDockWindows()
        {
            foreach (DockContent content in dockContents.Values)
                content.Hide();
        }

        private void LayoutPresetCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ignoreLayoutPresetChanges || layoutPresetCombo == null)
                return;

            LayoutPresetItem selected = layoutPresetCombo.SelectedItem as LayoutPresetItem;
            if (selected == null)
                return;

            switch (selected.Key)
            {
                case LayoutPresetDefault:
                    ApplyDefaultDockLayout();
                    break;
                case LayoutPresetClassic:
                    ApplyClassicDockLayout();
                    break;
                case LayoutPresetVertical:
                    ApplyVerticalLayout();
                    break;
            }

            EnsureFloatingWindowsVisible();
        }

        private void DockContent_DockStateChanged(object sender, EventArgs e)
        {
            EnsureFloatingWindowsVisible();
        }

        private void EnsureFloatingWindowsVisible()
        {
            Rectangle virtualBounds = SystemInformation.VirtualScreen;
            foreach (DockContent content in dockContents.Values)
            {
                if (content.DockState != DockState.Float || content.FloatPane == null || content.FloatPane.FloatWindow == null)
                    continue;

                Rectangle bounds = content.FloatPane.FloatWindow.Bounds;
                int width = Math.Min(Math.Max(bounds.Width, 220), virtualBounds.Width);
                int height = Math.Min(Math.Max(bounds.Height, 160), virtualBounds.Height);
                int x = Math.Max(virtualBounds.Left, Math.Min(bounds.Left, virtualBounds.Right - width));
                int y = Math.Max(virtualBounds.Top, Math.Min(bounds.Top, virtualBounds.Bottom - height));

                Rectangle fixedBounds = new Rectangle(x, y, width, height);
                if (fixedBounds != bounds)
                    content.FloatPane.FloatWindow.Bounds = fixedBounds;
            }
        }

        private sealed class LayoutPresetItem
        {
            public string Key { get; private set; }
            public string Title { get; private set; }

            public LayoutPresetItem(string key, string title)
            {
                Key = key;
                Title = title;
            }

            public override string ToString()
            {
                return Title;
            }
        }

        private sealed class NonClosableFloatWindowFactory : DockPanelExtender.IFloatWindowFactory
        {
            public FloatWindow CreateFloatWindow(DockPanel dockPanel, DockPane dockPane)
            {
                return new NonClosableFloatWindow(dockPanel, dockPane);
            }

            public FloatWindow CreateFloatWindow(DockPanel dockPanel, DockPane dockPane, Rectangle bounds)
            {
                return new NonClosableFloatWindow(dockPanel, dockPane, bounds);
            }
        }

        private sealed class NonClosableFloatWindow : FloatWindow
        {
            public NonClosableFloatWindow(DockPanel dockPanel, DockPane dockPane)
                : base(dockPanel, dockPane)
            {
                ApplyNoCloseStyle();
            }

            public NonClosableFloatWindow(DockPanel dockPanel, DockPane dockPane, Rectangle bounds)
                : base(dockPanel, dockPane, bounds)
            {
                ApplyNoCloseStyle();
            }

            private void ApplyNoCloseStyle()
            {
                ControlBox = false;
                MinimizeBox = false;
                MaximizeBox = false;
            }
        }

        public void SetPanel(UserControl np)
        {
            if (SelectedPanel == np) return;
            
            if (SelectedPanel != null)
                SelectedPanel.Parent = null;
            np.Dock = DockStyle.Fill;
            np.Size = PanelContainer.Size;
//            np.Size = PanelContainer.Size;
//            np.Location = new Point(0, 0);
//            np.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            SelectedPanel = np;
            if (SelectedPanel != null)
                SelectedPanel.Parent = PanelContainer;
        }

        private void saveLevelButton_Click(object sender, EventArgs e) {
            levelEditorControl1.UndoManager.Clean();
            Level.Save();
        }

        private void LevelEditor_FormClosing(object sender, FormClosingEventArgs e) {
            if (levelEditorControl1.UndoManager.dirty) {
                DialogResult dr;
                dr = MessageBox.Show(LanguageManager.Get("LevelEditor", "UnsavedLevel"), "NSMB Editor 5", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information);
                if (dr == DialogResult.Yes) {
                    Level.Save();
                } else if (dr == DialogResult.Cancel) {
                    e.Cancel = true;
                }
            }
            if (!e.Cancel)
            {
                ROM.fileBackups.Remove(Level.source.getBackupText());
                ROM.writeBackupSetting();
            }
        }

        private void viewMinimapButton_Click(object sender, EventArgs e)
        {
            if (minimapDockWindow != null)
            {
                minimapDockWindow.Show(editingDockPanel);
                minimapDockWindow.Activate();
            }
            else
            {
                MinimapForm.Show();
            }
        }

        public void LevelConfigForm_ReloadTileset() {
            GFX.LoadTilesets(Level.Blocks[0][0xC], Level.Blocks[2][2]);
            Level.ReRenderAll();

            Level.repaintAllTilemap();
            levelEditorControl1.updateTileCache(true);
            levelEditorControl1.repaint();

            oem.ReloadObjectPicker();
            Invalidate(true);
        }

        private void LevelEditor_FormClosed(object sender, FormClosedEventArgs e) {
            if (MinimapForm != null) {
                MinimapForm.Close();
            }

            if (tools != null)
                tools.Close();
            if (sprEvents != null)
                sprEvents.Close();
            GFX.close();
            Level.close();
        }

        private void smallBlockOverlaysToolStripMenuItem_Click(object sender, EventArgs e) {
            GFX.RepatchBlocks(smallBlockOverlaysToolStripMenuItem.Checked);
            Properties.Settings.Default.SmallBlockOverlays = smallBlockOverlaysToolStripMenuItem.Checked;
            Properties.Settings.Default.Save();
            Level.ReRenderAll();
            levelEditorControl1.updateTileCache(true);
            Invalidate(true);
        }

        private void showResizeHandles_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.ShowResizeHandles = showResizeHandles.Checked;
            Properties.Settings.Default.Save();
            oem.resizeHandles = showResizeHandles.Checked;
            Invalidate(true);
        }

        private void spriteFinder_Click(object sender, EventArgs e)
        {
            tools.Show();
            tools.BringToFront();
        }

        private void cutButton_Click(object sender, EventArgs e)
        {
            levelEditorControl1.cut();
        }

        private void copyButton_Click(object sender, EventArgs e)
        {
            levelEditorControl1.copy();
        }

        private void pasteButton_Click(object sender, EventArgs e)
        {
            levelEditorControl1.paste();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            levelEditorControl1.delete();
        }

        private void zoomMenu_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            foreach (ToolStripMenuItem it in zoomMenu.DropDown.Items)
                it.Checked = false;
            (e.ClickedItem as ToolStripMenuItem).Checked = true;

            String s = e.ClickedItem.Text;

            int ind = s.IndexOf(" %");
            s = s.Remove(ind);

            float z = Int32.Parse(s);
            levelEditorControl1.SetZoom(z / 100);
        }

        public void zoomOut()
        {
            int idx = findZoomItemIndex();
            if (idx < zoomMenu.DropDown.Items.Count - 1)
                zoomMenu.DropDown.Items[idx + 1].PerformClick();
        }

        public void zoomIn()
        {
            int idx = findZoomItemIndex();
            if (idx > 0)
                zoomMenu.DropDown.Items[idx - 1].PerformClick();
        }

        private int findZoomItemIndex()
        {
            for (int i = 0; i < zoomMenu.DropDown.Items.Count; i++)
                if ((zoomMenu.DropDown.Items[i] as ToolStripMenuItem).Checked)
                    return i;
            return -1;
        }

        private void editTileset_Click(object sender, EventArgs e)
        {
            try
            {
                new TilesetEditor(Level.Blocks[0][0xC], "").Show();
            }
            catch (AlreadyEditingException)
            {
                MessageBox.Show(LanguageManager.Get("Errors", "Tileset"));
            }
        }

        private void setBgImageButton_Click(object sender, EventArgs e)
        {
            if (openImage.ShowDialog() != DialogResult.OK)
                return;

            try
            {
                Image i = Image.FromFile(openImage.FileName, false);
                removeBgButton_Click(null, null);
                levelEditorControl1.bgImage = i;
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format(LanguageManager.Get("LevelEditor", "ImageError"), ex.Message));
            }
            levelEditorControl1.repaint();
        }

        private void removeBgButton_Click(object sender, EventArgs e)
        {
            if (levelEditorControl1.bgImage != null)
            {
                levelEditorControl1.bgImage.Dispose();
                levelEditorControl1.bgImage = null;
            }
            levelEditorControl1.repaint();
        }

        private void moveBGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (moveBGToolStripMenuItem.Checked)
                levelEditorControl1.SetEditionMode(bgdragem);
            else
                levelEditorControl1.SetEditionMode(oem);
        }

        private void splitContainer1_SplitterMoved(object sender, SplitterEventArgs e)
        {
            PanelContainer.Invalidate(true);
        }

        private void dsScreenShowButton_Click(object sender, EventArgs e)
        {
            levelEditorControl1.showDSScreen = dsScreenShowButton.Checked;
            levelEditorControl1.repaint();
        }

        private void snapToggleButton_Click(object sender, EventArgs e)
        {
            oem.snapTo8Pixels = snapToggleButton.Checked;
            oem.UpdateSelectionBounds();
        }

        private void showGridButton_Click(object sender, EventArgs e)
        {
            levelEditorControl1.showGrid = showGridButton.Checked;
            levelEditorControl1.repaint();
        }

        private FormWindowState prevState;

        private void fullScreenButton_CheckedChanged(object sender, EventArgs e)
        {
            if (fullScreenButton.Checked)
            {
                prevState = this.WindowState;
                this.WindowState = FormWindowState.Normal;
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                this.WindowState = FormWindowState.Maximized;
            }
            else
            {
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
                this.WindowState = prevState;
            }
        }

        public void ExitFullScreen()
        {
            fullScreenButton.Checked = false;
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            levelEditorControl1.delete();
        }

        private void lowerButton_Click(object sender, EventArgs e)
        {
            levelEditorControl1.lower();
        }

        private void raiseButton_Click(object sender, EventArgs e)
        {
            levelEditorControl1.raise();
        }

        private void backupTimer_Tick(object sender, EventArgs e)
        {
            if (!ROM.fileBackups.Contains(Level.source.getBackupText()))
            {
                ROM.fileBackups.Add(Level.source.getBackupText());
                ROM.writeBackupSetting();
            }
            levelSaver.RunWorkerAsync();
        }

        private void levelSaver_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                Console.Out.WriteLine("Backing up level " + Level.source.getBackupText());
                ExportedLevel exlvl = Level.getExport();
                string backupPath = System.IO.Path.Combine(Application.StartupPath, "Backup");
                if (!System.IO.Directory.Exists(backupPath))
                    System.IO.Directory.CreateDirectory(backupPath);
                string filename = Level.source.getBackupFileName();
                System.IO.FileStream fs = new System.IO.FileStream(System.IO.Path.Combine(backupPath, filename), System.IO.FileMode.Create);
                System.IO.BinaryWriter bw = new System.IO.BinaryWriter(fs);
                exlvl.Write(bw);
                bw.Close();
            }
            catch (Exception ex) { }
        }

        private void LevelEditor_SizeChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.LevelMaximized = this.WindowState == FormWindowState.Maximized;
            Properties.Settings.Default.Save();
        }

        private void spriteEvents_Click(object sender, EventArgs e)
        {
            sprEvents.Show();
            sprEvents.BringToFront();
            sprEvents.ReloadSprites(null, null);
        }

        private void ExtraDataBtn_Click(object sender, EventArgs e)
        {
            new ExtraDataEditor(this.levelEditorControl1).ShowDialog();
        }
    }
}
