using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace NSMBe5
{
    public partial class LevelChooser
    {
        private const int MaxRecentFiles = 10;
        private string currentLoadedRecentFile = null;
        private Dictionary<string, string> projectDisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly IRecentFilesStore recentFilesStore = new SettingsRecentFilesStore();
        private readonly IProjectDisplayNameStore projectDisplayNameStore = new FileProjectDisplayNameStore();

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
            if (projectsPanel == null) return;
            foreach (Control c in projectsPanel.Controls)
            {
                if (c is Panel panel && panel.Tag is string fp && fp == filePath)
                {
                    foreach (Control child in panel.Controls)
                    {
                        if (child is Label lbl && lbl.Tag is string t && t == "modifiedLbl")
                        {
                            try
                            {
                                lbl.AutoSize = false;
                                lbl.AutoEllipsis = true;
                                lbl.TextAlign = ContentAlignment.MiddleRight;
                                lbl.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                                lbl.Text = GetRelativeTimeString(File.GetLastWriteTime(filePath));
                            }
                            catch { }
                            return;
                        }
                    }
                }
            }
        }

        private void UpdateCardOpenedState()
        {
            if (projectsPanel == null) return;
            foreach (Control c in projectsPanel.Controls)
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

        private void LoadRecentFiles()
        {
            UpdateRecentFilesMenu();
        }

        private void AddToRecentFiles(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            var recentFiles = GetRecentFiles();
            recentFiles.Remove(filePath);
            recentFiles.Insert(0, filePath);

            while (recentFiles.Count > MaxRecentFiles)
                recentFiles.RemoveAt(recentFiles.Count - 1);

            recentFilesStore.Save(recentFiles);
            UpdateRecentFilesMenu();
        }

        private List<string> GetRecentFiles()
        {
            return recentFilesStore.LoadExisting();
        }

        private void UpdateRecentFilesMenu()
        {
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
                if (File.Exists(filePath))
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
                    recentFilesStore.Save(recentFiles);
                    UpdateRecentFilesMenu();
                }
            }
        }

        private void ClearRecentFiles_Click(object sender, EventArgs e)
        {
            recentFilesStore.Save(new List<string>());
            UpdateRecentFilesMenu();
        }

        private void UpdateRecentFilesPanel()
        {
            try
            {
                if (projectsPanel == null) return;
                EnsureProjectsPanelNoHorizontalScroll();

                projectsPanel.Controls.Clear();
                var recentFiles = GetRecentFiles();

                if (recentFiles.Count == 0)
                {
                    var lbl = new Label() { Text = "(No recent projects)", AutoSize = true, ForeColor = SystemColors.GrayText };
                    projectsPanel.Controls.Add(lbl);
                }
                else
                {
                    string filter = "";
                    if (searchBox != null)
                    {
                        if (searchBox.ForeColor == SystemColors.GrayText)
                            filter = "";
                        else
                            filter = searchBox.Text?.ToLowerInvariant() ?? "";
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
                        projectsPanel.Controls.Add(card);
                        projectsPanel.SetFlowBreak(card, true);
                    }
                }
                EnsureProjectsPanelNoHorizontalScroll();
            }
            catch { }
        }

        private int GetProjectsCardWidth()
        {
            if (projectsPanel == null) return 440;
            int safety = 2;
            int reservedForVScroll = SystemInformation.VerticalScrollBarWidth;
            int width = projectsPanel.ClientSize.Width - reservedForVScroll - safety;
            return Math.Max(240, width);
        }

        private void EnsureProjectsPanelNoHorizontalScroll()
        {
            if (projectsPanel == null) return;
            try
            {
                projectsPanel.HorizontalScroll.Maximum = 0;
                projectsPanel.HorizontalScroll.Visible = false;
                projectsPanel.HorizontalScroll.Enabled = false;

                if (projectsPanel.IsHandleCreated)
                    ShowScrollBar(projectsPanel.Handle, SB_HORZ, false);
            }
            catch { }
        }

        private string GetProjectDisplayNamesFilePath()
        {
            return Path.Combine(Application.StartupPath, "project_display_names.txt");
        }

        private void LoadProjectDisplayNames()
        {
            projectDisplayNames = projectDisplayNameStore.Load(GetProjectDisplayNamesFilePath());
        }

        private void SaveProjectDisplayNames()
        {
            projectDisplayNameStore.Save(GetProjectDisplayNamesFilePath(), projectDisplayNames);
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
                Height = 52,
                Margin = new Padding(8),
                BorderStyle = BorderStyle.None,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                MinimumSize = new Size(240, 52),
                BackColor = ThemePanel
            };

            var nameLbl = new Label
            {
                AutoSize = false,
                Location = new Point(10, 6),
                Size = new Size(panel.Width - 190, 18),
                AutoEllipsis = true,
                Font = GetModernUIFont(10F, FontStyle.Regular),
                ForeColor = ThemeTextPrimary,
                Text = GetProjectDisplayName(filePath),
                Tag = "nameLbl"
            };
            if (!string.IsNullOrEmpty(currentLoadedRecentFile) && string.Equals(currentLoadedRecentFile, filePath, StringComparison.OrdinalIgnoreCase))
            {
                try { nameLbl.Font = new Font(nameLbl.Font, FontStyle.Bold); } catch { }
            }
            panel.Controls.Add(nameLbl);

            var pathLbl = new Label
            {
                AutoSize = false,
                Location = new Point(10, 27),
                Size = new Size(panel.Width - 190, 16),
                AutoEllipsis = true,
                Font = GetModernUIFont(8.5F, FontStyle.Regular),
                ForeColor = ThemeTextSecondary,
                Text = filePath
            };
            panel.Controls.Add(pathLbl);

            int modWidth = Math.Max(90, Math.Min(320, panel.Width - 170));
            var modifiedLbl = new Label
            {
                AutoSize = false,
                AutoEllipsis = true,
                Location = new Point(panel.Width - modWidth - 10, 10),
                Size = new Size(modWidth, 28),
                Font = GetModernUIFont(8.5F, FontStyle.Regular),
                ForeColor = ThemeTextSecondary,
                TextAlign = ContentAlignment.MiddleRight,
                Text = GetRelativeTimeString(File.GetLastWriteTime(filePath)),
                Tag = "modifiedLbl",
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            panel.Controls.Add(modifiedLbl);

            StyleCardPanel(panel);

            try
            {
                if (toolTip1 != null)
                {
                    toolTip1.SetToolTip(pathLbl, filePath);
                    toolTip1.SetToolTip(modifiedLbl, File.GetLastWriteTime(filePath).ToString());
                }
            }
            catch { }

            panel.Tag = filePath;
            panel.DoubleClick += (s, e) => OpenProjectFromCard(filePath);
            nameLbl.DoubleClick += (s, e) => OpenProjectFromCard(filePath);
            pathLbl.DoubleClick += (s, e) => OpenProjectFromCard(filePath);

            var ctx = new ContextMenuStrip();
            var showItem = new ToolStripMenuItem(LanguageManager.Get("LevelChooser", "showInExplorerToolStripMenuItem") ?? "Show in Explorer") { Tag = filePath };
            showItem.Click += (s, e) => ShowInExplorerFor(filePath);
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
            removeItem.Click += (s, e) => RemoveProjectFor(filePath);
            ctx.Items.Add(showItem);
            ctx.Items.Add(setNameItem);
            ctx.Items.Add(removeItem);
            panel.ContextMenuStrip = ctx;

            return panel;
        }

        private void OpenProjectFromCard(string filePath)
        {
            if (!File.Exists(filePath)) return;
            LoadROMFromPath(filePath);
            MarkRecentOpened(filePath);
            UpdateCardModifiedLabel(filePath);
        }

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            if (searchBox.ForeColor == SystemColors.GrayText)
                return;

            UpdateRecentFilesPanel();
        }

        private void SearchBox_Enter(object sender, EventArgs e)
        {
            if (searchBox.ForeColor == SystemColors.GrayText)
            {
                searchBox.Text = string.Empty;
                searchBox.ForeColor = SystemColors.WindowText;
            }
        }

        private void SearchBox_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(searchBox.Text))
            {
                searchBox.ForeColor = SystemColors.GrayText;
                searchBox.Text = "Search";
            }
        }

        private void AddProjectButton_Click(object sender, EventArgs e)
        {
            if (openROMDialog.ShowDialog() == DialogResult.OK)
            {
                var path = openROMDialog.FileName;

                var recentFiles = GetRecentFiles();
                if (recentFiles.Any(f => string.Equals(f, path, StringComparison.OrdinalIgnoreCase)))
                {
                    OpenProjectFromCard(path);
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
                    OpenProjectFromCard(path);
                }
            }
        }

        private void RecentFilesListBox_DoubleClick(object sender, EventArgs e)
        {
            if (recentFilesListBox == null) return;
            if (recentFilesListBox.SelectedItem is string filePath)
            {
                if (File.Exists(filePath))
                {
                    OpenProjectFromCard(filePath);
                }
                else
                {
                    MessageBox.Show($"File not found: {filePath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    var recentFiles = GetRecentFiles();
                    recentFiles.Remove(filePath);
                    recentFilesStore.Save(recentFiles);
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
            if (projectsPanel == null) return;
            try
            {
                if (searchBox != null && addProjectButton != null)
                {
                    int availableWidth = projectsPanel.Width - addProjectButton.Width - 32;
                    searchBox.Width = Math.Max(100, availableWidth);
                }

                foreach (Control c in projectsPanel.Controls)
                {
                    if (c is Panel panel)
                    {
                        projectsPanel.SetFlowBreak(panel, true);
                        int newWidth = GetProjectsCardWidth();
                        panel.Width = newWidth;
                        foreach (Control child in panel.Controls)
                        {
                            if (child is Label lbl)
                            {
                                if (lbl.Tag is string t && t == "modifiedLbl")
                                {
                                    int avail = Math.Max(80, panel.Width - 160);
                                    int modLblWidth = Math.Min(300, avail);
                                    lbl.Location = new Point(panel.Width - modLblWidth - 8, lbl.Location.Y);
                                    lbl.Width = modLblWidth;
                                }
                                else
                                {
                                    lbl.Width = panel.Width - 180;
                                }
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
                bool exists = File.Exists(filePath);
                showInExplorerToolStripMenuItem.Enabled = exists;
                removeProjectToolStripMenuItem.Enabled = true;
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void ShowInExplorerMenuItem_Click(object sender, EventArgs e)
        {
            if (recentFilesListBox == null) return;
            if (recentFilesListBox.SelectedItem is string filePath)
                ShowInExplorerFor(filePath);
        }

        private void ShowInExplorerFor(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            if (File.Exists(filePath))
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
                recentFilesStore.Save(recentFiles);
                UpdateRecentFilesMenu();
            }
        }

        private void RemoveProjectFor(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            var recentFiles = GetRecentFiles();
            recentFiles.Remove(filePath);
            recentFilesStore.Save(recentFiles);

            if (!string.IsNullOrEmpty(currentLoadedRecentFile) && string.Equals(currentLoadedRecentFile, filePath, StringComparison.OrdinalIgnoreCase))
                currentLoadedRecentFile = null;

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
                RemoveProjectFor(filePath);
        }
    }
}
