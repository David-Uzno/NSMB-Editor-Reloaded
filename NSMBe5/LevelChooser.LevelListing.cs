using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace NSMBe5
{
    public partial class LevelChooser
    {
        private sealed class LevelListingItem
        {
            public string World;
            public string Level;
            public string DisplayName;
            public string InternalName;
            public string ItemType;
            public string Category;
        }

        private readonly List<LevelListingItem> allLevelListingItems = new List<LevelListingItem>();
        private string activeLevelInternalName;
        private bool levelListingUiInitialized;
        private Panel levelListingToolbar;
        private Label levelListingHeader;
        private TextBox levelListingSearchBox;
        private Label levelTypeFilterLabel;
        private ComboBox levelTypeFilter;
        private Panel levelPreviewPanel;
        private Label levelPreviewHeader;
        private PictureBox levelPreviewAdvancedPicture;
        private PictureBox levelPreviewPicture;
        private Label levelPreviewName;
        private Label levelPreviewMeta;
        private Label previewModeLabel;
        private ComboBox previewModeComboBox;
        private bool previewModeSelectionInternalUpdate;
        private int previousPreviewModeIndex = (int)LevelListingPreviewMode.Advanced;

        private enum LevelListingPreviewMode
        {
            MiniMap = 0,
            Advanced = 1,
            AdvancedReduced = 2,
            None = 3
        }

        private static string GetLevelChooserText(string key, string fallback)
        {
            string value = LanguageManager.Get("LevelChooser", key);
            if (string.IsNullOrEmpty(value) || value.StartsWith("<ERROR", StringComparison.Ordinal))
                return fallback;

            return value;
        }

        private static string GetPreviewModeText(LevelListingPreviewMode mode)
        {
            switch (mode)
            {
                case LevelListingPreviewMode.MiniMap:
                    return GetLevelChooserText("LevelListingPreviewOptionMiniMap", "MiniMap");
                case LevelListingPreviewMode.Advanced:
                    return GetLevelChooserText("LevelListingPreviewOptionComplete", "Complete");
                case LevelListingPreviewMode.AdvancedReduced:
                    return GetLevelChooserText("LevelListingPreviewOptionCompleteReduced", "Complete reduced experimental");
                case LevelListingPreviewMode.None:
                    return GetLevelChooserText("LevelListingPreviewOptionNone", "None");
                default:
                    return mode.ToString();
            }
        }

        private void InitializeModernLevelListingUI()
        {
            if (levelListingUiInitialized || tabPage2 == null || levelTreeView == null)
                return;

            levelListingUiInitialized = true;

            levelListingToolbar = new Panel
            {
                Name = "levelListingToolbar",
                BackColor = ThemeWhite,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            levelListingHeader = new Label
            {
                Name = "levelListingHeader",
                AutoSize = false,
                Text = GetLevelChooserText("LevelListingHeader", "Scenes"),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = GetModernUIFont(10.5f, FontStyle.Bold),
                ForeColor = ThemeTextPrimary
            };

            levelListingSearchBox = new TextBox
            {
                Name = "levelListingSearchBox",
                BorderStyle = BorderStyle.FixedSingle,
                Font = GetModernUIFont(9f, FontStyle.Regular),
                Text = "Search",
                BackColor = ThemeWhite,
                ForeColor = SystemColors.GrayText
            };
            if (searchBox != null)
            {
                levelListingSearchBox.Margin = searchBox.Margin;
                levelListingSearchBox.Padding = searchBox.Padding;
            }
            levelListingSearchBox.TextChanged += LevelFilterControl_Changed;
            levelListingSearchBox.Enter += LevelListingSearchBox_Enter;
            levelListingSearchBox.Leave += LevelListingSearchBox_Leave;

            levelTypeFilter = new ComboBox
            {
                Name = "levelTypeFilter",
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = GetModernUIFont(9f, FontStyle.Regular)
            };
            levelTypeFilterLabel = new Label
            {
                Name = "levelTypeFilterLabel",
                AutoSize = false,
                Text = "Type:",
                TextAlign = ContentAlignment.MiddleRight,
                Font = GetModernUIFont(9f, FontStyle.Regular),
                ForeColor = ThemeTextPrimary
            };
            levelTypeFilter.Items.AddRange(new object[] { "All", "Levels", "Towers", "Castles", "Ghost Houses", "Cannons", "Toad Houses", "Versus", "Unused" });
            levelTypeFilter.SelectedIndex = 0;
            levelTypeFilter.SelectedIndexChanged += LevelFilterControl_Changed;

            levelListingToolbar.Controls.Add(levelListingHeader);
            levelListingToolbar.Controls.Add(levelListingSearchBox);
            levelListingToolbar.Controls.Add(levelTypeFilterLabel);
            levelListingToolbar.Controls.Add(levelTypeFilter);

            levelPreviewPanel = new Panel
            {
                Name = "levelPreviewPanel",
                BackColor = ThemePanel,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom
            };

            levelPreviewHeader = new Label
            {
                AutoSize = false,
                Text = GetLevelChooserText("LevelListingPreviewHeader", "Preview"),
                Font = GetModernUIFont(9.5f, FontStyle.Bold),
                ForeColor = ThemeTextPrimary,
                TextAlign = ContentAlignment.MiddleLeft
            };

            levelPreviewPicture = new PictureBox
            {
                BackColor = ThemeWhite,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                Visible = false
            };

            levelPreviewAdvancedPicture = new PictureBox
            {
                BackColor = ThemeWhite,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                Visible = false
            };

            levelPreviewName = new Label
            {
                AutoSize = false,
                ForeColor = ThemeTextPrimary,
                Font = GetModernUIFont(9f, FontStyle.Bold),
                Text = "",
                TextAlign = ContentAlignment.TopLeft
            };

            levelPreviewMeta = new Label
            {
                AutoSize = false,
                ForeColor = ThemeTextSecondary,
                Font = GetModernUIFont(8.5f, FontStyle.Regular),
                Text = "",
                TextAlign = ContentAlignment.TopLeft
            };

            levelPreviewPanel.Controls.Add(levelPreviewHeader);
            levelPreviewPanel.Controls.Add(levelPreviewAdvancedPicture);
            levelPreviewPanel.Controls.Add(levelPreviewPicture);
            levelPreviewPanel.Controls.Add(levelPreviewName);
            levelPreviewPanel.Controls.Add(levelPreviewMeta);

            tabPage2.Controls.Add(levelListingToolbar);
            tabPage2.Controls.Add(levelPreviewPanel);

            levelTreeView.HideSelection = false;
            levelTreeView.FullRowSelect = true;
            levelTreeView.ShowNodeToolTips = false;
            levelTreeView.LineColor = Color.FromArgb(210, 210, 210);
            levelTreeView.ImageList = null;

            tabPage2.Resize += TabPage2_Resize;

            LayoutModernLevelListingUI();
        }

        private void InitializePreviewModeOptionUI()
        {
            if (groupBox1 == null || previewModeComboBox != null)
                return;

            previewModeLabel = new Label
            {
                Name = "previewModeLabel",
                AutoSize = true,
                Text = GetLevelChooserText("LevelListingPreviewModeLabel", "Level Listing Preview")
            };

            previewModeComboBox = new ComboBox
            {
                Name = "previewModeComboBox",
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            previewModeComboBox.Items.AddRange(new object[]
            {
                GetPreviewModeText(LevelListingPreviewMode.MiniMap),
                GetPreviewModeText(LevelListingPreviewMode.Advanced),
                GetPreviewModeText(LevelListingPreviewMode.AdvancedReduced),
                GetPreviewModeText(LevelListingPreviewMode.None)
            });

            int configuredMode = Properties.Settings.Default.LevelListingPreviewMode;
            if (configuredMode < 0 || configuredMode > 3)
                configuredMode = (int)LevelListingPreviewMode.Advanced;
            previewModeComboBox.SelectedIndex = configuredMode;
            previousPreviewModeIndex = configuredMode;
            previewModeComboBox.SelectedIndexChanged += PreviewModeComboBox_SelectedIndexChanged;

            previewModeLabel.Location = new Point(125, 75);
            previewModeLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            previewModeComboBox.Location = new Point(125, 92);
            previewModeComboBox.Size = new Size(160, 24);
            previewModeComboBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            groupBox1.Controls.Add(previewModeLabel);
            groupBox1.Controls.Add(previewModeComboBox);
        }

        private LevelListingPreviewMode GetCurrentPreviewMode()
        {
            int rawMode = Properties.Settings.Default.LevelListingPreviewMode;
            if (previewModeComboBox != null && previewModeComboBox.SelectedIndex >= 0)
                rawMode = previewModeComboBox.SelectedIndex;

            if (rawMode < 0 || rawMode > 3)
                rawMode = (int)LevelListingPreviewMode.Advanced;

            return (LevelListingPreviewMode)rawMode;
        }

        private void ApplyLevelListingPreviewMode(bool refreshPreview)
        {
            if (!levelListingUiInitialized || levelPreviewPanel == null)
                return;

            LevelListingPreviewMode mode = GetCurrentPreviewMode();
            bool shouldShowPreviewPanel = romLoaded && mode != LevelListingPreviewMode.None;
            levelPreviewPanel.Visible = shouldShowPreviewPanel;

            if (!shouldShowPreviewPanel)
            {
                levelPreviewName.Text = "";
                levelPreviewMeta.Text = "";
                levelPreviewAdvancedPicture.Visible = false;
                levelPreviewAdvancedPicture.Image = null;
                levelPreviewPicture.Visible = false;
                levelPreviewPicture.Image = null;
            }

            LayoutModernLevelListingUI();

            if (refreshPreview && shouldShowPreviewPanel)
            {
                string selectedInternalName = levelTreeView != null && levelTreeView.SelectedNode != null
                    ? levelTreeView.SelectedNode.Tag as string
                    : null;
                UpdateLevelPreview(selectedInternalName);
            }
        }

        private void PreviewModeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (previewModeComboBox == null || previewModeComboBox.SelectedIndex < 0 || previewModeSelectionInternalUpdate)
                return;

            int newIndex = previewModeComboBox.SelectedIndex;
            if (newIndex == (int)LevelListingPreviewMode.Advanced && previousPreviewModeIndex != (int)LevelListingPreviewMode.Advanced)
            {
                DialogResult confirm = ShowPreviewModeWarning(
                    GetLevelChooserText("LevelListingPreviewWarningComplete", "Complete preview may be slower. Continue?"),
                    GetLevelChooserText("LevelListingPreviewWarningTitle", "Preview Mode Warning"));

                if (confirm == DialogResult.No)
                {
                    previewModeSelectionInternalUpdate = true;
                    previewModeComboBox.SelectedIndex = previousPreviewModeIndex;
                    previewModeSelectionInternalUpdate = false;
                    return;
                }
            }
            else if (newIndex == (int)LevelListingPreviewMode.AdvancedReduced && previousPreviewModeIndex != (int)LevelListingPreviewMode.AdvancedReduced)
            {
                DialogResult confirm = ShowPreviewModeWarning(
                    GetLevelChooserText("LevelListingPreviewWarningCompleteReduced", "Complete reduced experimental uses fewer resources than Complete, but may still be slower than MiniMap. In some levels, the camera used for the preview may be in the wrong position. Continue?"),
                    GetLevelChooserText("LevelListingPreviewWarningTitle", "Preview Mode Warning"));

                if (confirm == DialogResult.No)
                {
                    previewModeSelectionInternalUpdate = true;
                    previewModeComboBox.SelectedIndex = previousPreviewModeIndex;
                    previewModeSelectionInternalUpdate = false;
                    return;
                }
            }

            Properties.Settings.Default.LevelListingPreviewMode = newIndex;
            Properties.Settings.Default.Save();
            previousPreviewModeIndex = newIndex;

            ApplyLevelListingPreviewMode(true);
        }

        private DialogResult ShowPreviewModeWarning(string message, string title)
        {
            using (Form warningDialog = new Form())
            {
                warningDialog.Text = title;
                warningDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                warningDialog.StartPosition = FormStartPosition.CenterParent;
                warningDialog.MinimizeBox = false;
                warningDialog.MaximizeBox = false;
                warningDialog.ShowInTaskbar = false;
                warningDialog.ClientSize = new Size(430, 145);

                Label messageLabel = new Label
                {
                    AutoSize = false,
                    Text = message,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Location = new Point(12, 12),
                    Size = new Size(406, 78)
                };

                Button yesButton = new Button
                {
                    Text = GetLevelChooserText("LevelListingPreviewWarningYes", "Yes"),
                    DialogResult = DialogResult.Yes,
                    Size = new Size(90, 28),
                    Location = new Point(232, 102)
                };

                Button noButton = new Button
                {
                    Text = GetLevelChooserText("LevelListingPreviewWarningNo", "No"),
                    DialogResult = DialogResult.No,
                    Size = new Size(90, 28),
                    Location = new Point(328, 102)
                };

                warningDialog.Controls.Add(messageLabel);
                warningDialog.Controls.Add(yesButton);
                warningDialog.Controls.Add(noButton);
                warningDialog.AcceptButton = yesButton;
                warningDialog.CancelButton = noButton;

                return warningDialog.ShowDialog(this);
            }
        }

        private void TabPage2_Resize(object sender, EventArgs e)
        {
            LayoutModernLevelListingUI();
        }

        private void LayoutModernLevelListingUI()
        {
            if (!levelListingUiInitialized)
                return;

            int margin = 8;
            int toolbarHeight = 56;
            int gap = 8;
            int previewWidth = 210;
            bool showPreviewPanel = levelPreviewPanel != null && levelPreviewPanel.Visible;

            levelListingToolbar.SetBounds(margin, margin, tabPage2.ClientSize.Width - margin * 2, toolbarHeight);

            levelListingHeader.SetBounds(0, 0, 120, 24);
            int filterWidth = 95;
            int filterLabelWidth = 42;
            levelTypeFilter.SetBounds(Math.Max(228, levelListingToolbar.Width - filterWidth), 28, filterWidth, 24);
            if (levelTypeFilterLabel != null)
                levelTypeFilterLabel.SetBounds(levelTypeFilter.Left - filterLabelWidth - 4, 28, filterLabelWidth, 24);

            int searchX = 0;
            int searchY = 30;
            int searchHeight = 22;

            if (searchBox != null)
            {
                searchX = Math.Max(0, searchBox.Left - levelListingToolbar.Left);
                searchY = Math.Max(0, searchBox.Top - levelListingToolbar.Top);
                searchHeight = searchBox.Height;
            }

            int searchRight = (levelTypeFilterLabel != null ? levelTypeFilterLabel.Left : levelTypeFilter.Left) - 8;
            int searchWidth = Math.Max(220, searchRight - searchX);

            levelListingSearchBox.SetBounds(searchX, searchY, searchWidth, searchHeight);

            int contentTop = levelListingToolbar.Bottom + 6;
            int contentBottom = importLevelButton.Top - 8;
            int contentHeight = Math.Max(120, contentBottom - contentTop);
            int treeWidth;
            if (showPreviewPanel)
            {
                treeWidth = tabPage2.ClientSize.Width - margin * 2 - previewWidth - gap;
                if (treeWidth < 280)
                {
                    previewWidth = 180;
                    treeWidth = tabPage2.ClientSize.Width - margin * 2 - previewWidth - gap;
                }
            }
            else
            {
                previewWidth = 0;
                treeWidth = tabPage2.ClientSize.Width - margin * 2;
            }

            levelTreeView.SetBounds(margin, contentTop, treeWidth, contentHeight);
            if (showPreviewPanel)
                levelPreviewPanel.SetBounds(levelTreeView.Right + gap, contentTop, previewWidth, contentHeight);

            int inner = 8;
            if (!showPreviewPanel)
                return;

            levelPreviewHeader.SetBounds(inner, inner, levelPreviewPanel.Width - inner * 2, 20);

            int nextTop = levelPreviewHeader.Bottom + 6;
            bool showAdvancedImageBox = levelPreviewAdvancedPicture != null && levelPreviewAdvancedPicture.Visible && levelPreviewAdvancedPicture.Image != null;
            if (showAdvancedImageBox)
            {
                levelPreviewAdvancedPicture.SetBounds(inner, nextTop, levelPreviewPanel.Width - inner * 2, 88);
                nextTop = levelPreviewAdvancedPicture.Bottom + 6;
            }
            else
            {
                levelPreviewAdvancedPicture.SetBounds(0, 0, 0, 0);
            }

            bool showImageBox = levelPreviewPicture != null && levelPreviewPicture.Visible && levelPreviewPicture.Image != null;
            int nameTop;
            if (showImageBox)
            {
                levelPreviewPicture.SetBounds(inner, nextTop, levelPreviewPanel.Width - inner * 2, 88);
                nameTop = levelPreviewPicture.Bottom + 6;
            }
            else
            {
                levelPreviewPicture.SetBounds(0, 0, 0, 0);
                nameTop = nextTop;
            }

            levelPreviewName.SetBounds(inner, nameTop, levelPreviewPanel.Width - inner * 2, 22);
            levelPreviewMeta.SetBounds(inner, levelPreviewName.Bottom + 2, levelPreviewPanel.Width - inner * 2, Math.Max(40, levelPreviewPanel.Height - levelPreviewName.Bottom - 12));
        }

        private void LevelFilterControl_Changed(object sender, EventArgs e)
        {
            if (sender == levelListingSearchBox && levelListingSearchBox != null && levelListingSearchBox.ForeColor == SystemColors.GrayText)
                return;

            RefreshLevelTree();
        }

        private void LevelListingSearchBox_Enter(object sender, EventArgs e)
        {
            if (levelListingSearchBox == null)
                return;

            if (levelListingSearchBox.ForeColor == SystemColors.GrayText)
            {
                levelListingSearchBox.Text = string.Empty;
                levelListingSearchBox.ForeColor = SystemColors.WindowText;
            }
        }

        private void LevelListingSearchBox_Leave(object sender, EventArgs e)
        {
            if (levelListingSearchBox == null)
                return;

            if (string.IsNullOrWhiteSpace(levelListingSearchBox.Text))
            {
                levelListingSearchBox.ForeColor = SystemColors.GrayText;
                levelListingSearchBox.Text = "Search";
            }
        }

        private void LevelTreeView_NodeMouseHover(object sender, TreeNodeMouseHoverEventArgs e)
        {
        }

        private void LevelTreeView_MouseLeave(object sender, EventArgs e)
        {
            if (levelTreeView.SelectedNode == null || levelTreeView.SelectedNode.Tag == null)
                return;

            UpdateLevelPreview(levelTreeView.SelectedNode.Tag as string);
        }

        private bool MatchesLevelFilter(LevelListingItem item)
        {
            if (item == null)
                return false;

            string query = string.Empty;
            if (levelListingSearchBox != null)
            {
                query = levelListingSearchBox.ForeColor == SystemColors.GrayText
                    ? string.Empty
                    : levelListingSearchBox.Text.Trim();
            }
            if (!string.IsNullOrEmpty(query))
            {
                bool textMatch =
                    item.World.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.Level.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.DisplayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.InternalName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;

                if (!textMatch)
                    return false;
            }

            string typeSelection = levelTypeFilter == null ? "All" : (levelTypeFilter.SelectedItem as string ?? "All");
            if (!string.Equals(typeSelection, "All", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(item.Category, typeSelection, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        private static string DetermineLevelCategory(string world, string levelName, string internalName)
        {
            string worldText = (world ?? string.Empty).ToLowerInvariant();
            string levelText = (levelName ?? string.Empty).ToLowerInvariant();
            string internalText = (internalName ?? string.Empty).ToUpperInvariant();

            int underscoreIdx = internalText.IndexOf('_');
            if (underscoreIdx > 0)
                internalText = internalText.Substring(0, underscoreIdx);
            int atIdx = internalText.IndexOf('@');
            if (atIdx > 0)
                internalText = internalText.Substring(0, atIdx);

            int jNumber = 0;
            bool isJRange = internalText.Length >= 3 && internalText[0] == 'J' && int.TryParse(internalText.Substring(1), out jNumber);

            if (worldText.Contains("unused") || levelText.Contains("unused") || (isJRange && jNumber >= 6))
                return "Unused";

            if (worldText.Contains("toad") || levelText.Contains("toad") || internalText.StartsWith("I"))
                return "Toad Houses";

            if (worldText.Contains("versus") || worldText.Contains("vs.") || worldText.Contains("vs") ||
                levelText.Contains("versus") || levelText.Contains("vs.") || levelText.Contains("vs") ||
                (isJRange && jNumber >= 1 && jNumber <= 5))
                return "Versus";

            if (levelText.Contains("ghost"))
                return "Ghost Houses";

            if (levelText.Contains("tower"))
                return "Towers";

            if (levelText.Contains("castle"))
                return "Castles";

            if (levelText.Contains("cannon"))
                return "Cannons";

            return "Levels";
        }

        private TreeNode BuildLeafNode(LevelListingItem item)
        {
            TreeNode node = new TreeNode(item.DisplayName)
            {
                Tag = item.InternalName
            };

            node.ForeColor = ThemeTextPrimary;
            return node;
        }

        private void RefreshLevelTree()
        {
            if (levelTreeView == null)
                return;

            string selectedInternalName = levelTreeView.SelectedNode != null ? levelTreeView.SelectedNode.Tag as string : null;
            levelTreeView.BeginUpdate();
            levelTreeView.Nodes.Clear();

            var filteredItems = allLevelListingItems.Where(MatchesLevelFilter).ToList();
            var worldMap = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase);
            var levelFolderMap = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase);

            foreach (LevelListingItem item in filteredItems)
            {
                if (!worldMap.TryGetValue(item.World, out TreeNode worldNode))
                {
                    worldNode = levelTreeView.Nodes.Add(item.World);
                    worldMap[item.World] = worldNode;
                }

                if (string.Equals(item.ItemType, "Area", StringComparison.OrdinalIgnoreCase))
                {
                    string folderKey = item.World + "|" + item.Level;
                    if (!levelFolderMap.TryGetValue(folderKey, out TreeNode levelFolder))
                    {
                        levelFolder = worldNode.Nodes.Add(item.Level);
                        levelFolderMap[folderKey] = levelFolder;
                    }

                    levelFolder.Nodes.Add(BuildLeafNode(item));
                }
                else
                {
                    worldNode.Nodes.Add(BuildLeafNode(item));
                }
            }

            if (!string.IsNullOrEmpty(selectedInternalName))
            {
                TreeNode node = FindNodeByTag(levelTreeView.Nodes, selectedInternalName);
                if (node != null)
                {
                    levelTreeView.SelectedNode = node;
                    ExpandNodeAncestors(node);
                }
            }

            levelTreeView.EndUpdate();

            if (levelTreeView.SelectedNode != null && levelTreeView.SelectedNode.Tag != null)
                UpdateLevelPreview(levelTreeView.SelectedNode.Tag as string);
            else if (allLevelListingItems.Count == 0)
                UpdateLevelPreview(null);
        }

        private TreeNode FindNodeByTag(TreeNodeCollection nodes, string internalName)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Tag is string tag && string.Equals(tag, internalName, StringComparison.OrdinalIgnoreCase))
                    return node;

                TreeNode child = FindNodeByTag(node.Nodes, internalName);
                if (child != null)
                    return child;
            }

            return null;
        }

        private static void ExpandNodeAncestors(TreeNode node)
        {
            TreeNode current = node != null ? node.Parent : null;
            while (current != null)
            {
                current.Expand();
                current = current.Parent;
            }
        }


        private void SetActiveLevelFromSelection()
        {
            if (levelTreeView == null || levelTreeView.SelectedNode == null || !(levelTreeView.SelectedNode.Tag is string selectedInternalName))
                return;

            activeLevelInternalName = selectedInternalName;
            RefreshLevelTree();
        }

        private void LoadLevelNames()
        {
            List<string> levelNames = LanguageManager.GetList("LevelNames");

            allLevelListingItems.Clear();
            ClearPreviewCaches();

            string currentWorld = "World";
            string currentWorldPrefix = string.Empty;
            for (int nameIdx = 0; nameIdx < levelNames.Count; nameIdx++)
            {
                string line = levelNames[nameIdx].Trim();
                if (line == "") continue;

                if (line[0] == '-')
                {
                    string[] parseWorld = line.Substring(1).Split('|');
                    currentWorld = parseWorld.Length > 0 && !string.IsNullOrWhiteSpace(parseWorld[0]) ? parseWorld[0] : "World";
                    currentWorldPrefix = parseWorld.Length > 1 ? parseWorld[1].Trim() : string.Empty;
                    continue;
                }

                string[] parseLevel = line.Split('|');
                if (parseLevel.Length < 2)
                    continue;

                string levelName = parseLevel[0];

                int compactAreaCount = 0;
                bool usesCompactFormat =
                    parseLevel.Length >= 3 &&
                    parseLevel[1].IndexOf('_') < 0 &&
                    !string.IsNullOrEmpty(currentWorldPrefix) &&
                    int.TryParse(parseLevel[2], out compactAreaCount);

                if (usesCompactFormat)
                {
                    string compactLevelNumber = parseLevel[1].Trim();
                    if (!int.TryParse(compactLevelNumber, out int levelNumber))
                        continue;

                    string baseInternalName = currentWorldPrefix + levelNumber.ToString("D2");
                    if (compactAreaCount <= 1)
                    {
                        LevelListingItem item = new LevelListingItem
                        {
                            World = currentWorld,
                            Level = levelName,
                            DisplayName = levelName,
                            InternalName = baseInternalName + "_1",
                            ItemType = "Level",
                            Category = DetermineLevelCategory(currentWorld, levelName, baseInternalName + "_1")
                        };
                        allLevelListingItems.Add(item);
                    }
                    else
                    {
                        for (int areaIdx = 1; areaIdx <= compactAreaCount; areaIdx++)
                        {
                            string areaDisplayName = LanguageManager.Get("LevelChooser", "Area") + " " + areaIdx.ToString();
                            string internalName = baseInternalName + "_" + areaIdx.ToString();
                            LevelListingItem item = new LevelListingItem
                            {
                                World = currentWorld,
                                Level = levelName,
                                DisplayName = areaDisplayName,
                                InternalName = internalName,
                                ItemType = "Area",
                                Category = DetermineLevelCategory(currentWorld, levelName, internalName)
                            };
                            allLevelListingItems.Add(item);
                        }
                    }

                    continue;
                }

                int areaCount = parseLevel.Length - 1;
                if (areaCount == 1)
                {
                    string internalName = parseLevel[1];
                    LevelListingItem item = new LevelListingItem
                    {
                        World = currentWorld,
                        Level = levelName,
                        DisplayName = levelName,
                        InternalName = internalName,
                        ItemType = "Level",
                        Category = DetermineLevelCategory(currentWorld, levelName, internalName)
                    };
                    allLevelListingItems.Add(item);
                }
                else
                {
                    for (int areaIdx = 1; areaIdx <= areaCount; areaIdx++)
                    {
                        string areaDisplayName = LanguageManager.Get("LevelChooser", "Area") + " " + areaIdx.ToString();
                        string internalName = parseLevel[areaIdx];
                        LevelListingItem item = new LevelListingItem
                        {
                            World = currentWorld,
                            Level = levelName,
                            DisplayName = areaDisplayName,
                            InternalName = internalName,
                            ItemType = "Area",
                            Category = DetermineLevelCategory(currentWorld, levelName, internalName)
                        };
                        allLevelListingItems.Add(item);
                    }
                }
            }

            RefreshLevelTree();
        }
    }
}
