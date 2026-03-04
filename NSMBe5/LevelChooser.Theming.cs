using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NSMBe5
{
    public partial class LevelChooser
    {
        private static readonly Color ThemeWhite = ColorTranslator.FromHtml("#FFFFFF");
        private static readonly Color ThemePanel = ColorTranslator.FromHtml("#F5F5F5");
        private static readonly Color ThemeTextPrimary = ColorTranslator.FromHtml("#333333");
        private static readonly Color ThemeTextSecondary = ColorTranslator.FromHtml("#666666");
        private static readonly Color ThemeHover = ColorTranslator.FromHtml("#EEEEEE");
        private static readonly Color ThemePressed = ColorTranslator.FromHtml("#E6E6E6");
        private readonly HashSet<Button> themedButtons = new HashSet<Button>();
        private readonly HashSet<Panel> themedCards = new HashSet<Panel>();

        private Font GetModernUIFont(float size, FontStyle style)
        {
            try
            {
                string name = string.IsNullOrWhiteSpace(Properties.Settings.Default.UIFont)
                    ? "Segoe UI"
                    : Properties.Settings.Default.UIFont;
                return new Font(name, size, style);
            }
            catch
            {
                return new Font("Segoe UI", size, style);
            }
        }

        private GraphicsPath CreateRoundedRectPath(RectangleF rect, float radius)
        {
            var gp = new GraphicsPath();
            float d = Math.Max(0.1f, radius * 2f);

            float maxR = Math.Min(rect.Width, rect.Height) / 2f;
            if (radius > maxR) radius = maxR;

            gp.StartFigure();
            gp.AddArc(rect.Left, rect.Top, d, d, 180f, 90f);
            gp.AddArc(rect.Right - d, rect.Top, d, d, 270f, 90f);
            gp.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0f, 90f);
            gp.AddArc(rect.Left, rect.Bottom - d, d, d, 90f, 90f);
            gp.CloseFigure();
            return gp;
        }

        private void ApplyRoundedRegion(Control c, int radius)
        {
            if (c.Width <= 0 || c.Height <= 0) return;
            var rectf = new RectangleF(0f, 0f, c.Width, c.Height);
            using (var gp = CreateRoundedRectPath(rectf, radius))
            {
                if (c.Region != null) c.Region.Dispose();
                c.Region = new Region(gp);
            }
        }

        private void StyleButton(Button btn)
        {
            if (btn == null) return;

            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = ThemeWhite;
            btn.ForeColor = ThemeTextPrimary;
            btn.Cursor = Cursors.Hand;
            btn.Font = GetModernUIFont(9f, FontStyle.Regular);

            ApplyRoundedRegion(btn, 5);

            if (!themedButtons.Contains(btn))
            {
                themedButtons.Add(btn);

                btn.Resize += (s, e) => ApplyRoundedRegion(btn, 5);
                btn.MouseEnter += (s, e) => { if (btn.Enabled) btn.BackColor = ThemeHover; };
                btn.MouseLeave += (s, e) => { if (btn.Enabled) btn.BackColor = ThemeWhite; };
                btn.MouseDown += (s, e) => { if (btn.Enabled && e.Button == MouseButtons.Left) btn.BackColor = ThemePressed; };
                btn.MouseUp += (s, e) => { if (btn.Enabled) btn.BackColor = btn.ClientRectangle.Contains(btn.PointToClient(Cursor.Position)) ? ThemeHover : ThemeWhite; };
                btn.EnabledChanged += (s, e) =>
                {
                    btn.BackColor = btn.Enabled ? ThemeWhite : Color.FromArgb(248, 248, 248);
                    btn.ForeColor = btn.Enabled ? ThemeTextPrimary : Color.FromArgb(170, 170, 170);
                };

                btn.Paint += (s, pe) =>
                {
                    var b = (Button)s;
                    pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    pe.Graphics.PixelOffsetMode = PixelOffsetMode.Half;

                    var r = new RectangleF(0.5f, 0.5f, b.Width - 1f, b.Height - 1f);
                    using (var gp = CreateRoundedRectPath(r, 5f))
                    using (var pen = new Pen(b.Enabled ? Color.FromArgb(220, 220, 220) : Color.FromArgb(200, 200, 200), 1f))
                    {
                        pen.Alignment = PenAlignment.Inset;
                        pen.LineJoin = LineJoin.Round;
                        pen.StartCap = LineCap.Round;
                        pen.EndCap = LineCap.Round;

                        pe.Graphics.DrawPath(pen, gp);
                    }
                };
            }
        }

        private void StyleCardPanel(Panel card)
        {
            if (card == null) return;
            card.BackColor = ThemePanel;
            card.BorderStyle = BorderStyle.None;
            ApplyRoundedRegion(card, 6);

            if (!themedCards.Contains(card))
            {
                themedCards.Add(card);
                card.Resize += (s, e) => ApplyRoundedRegion(card, 6);

                void enter() => card.BackColor = ThemeHover;
                void leave() => card.BackColor = ThemePanel;

                void AttachHandlers(Control c)
                {
                    c.MouseEnter += (s, e) => enter();
                    c.MouseLeave += (s, e) =>
                    {
                        Point p = card.PointToClient(Cursor.Position);
                        if (card.ClientRectangle.Contains(p))
                            enter();
                        else
                            leave();
                    };

                    foreach (Control child in c.Controls)
                        AttachHandlers(child);
                }

                AttachHandlers(card);
                card.ControlAdded += (s, e) => AttachHandlers(e.Control);
            }
        }

        private void ApplyThemeRecursive(Control root)
        {
            foreach (Control c in root.Controls)
            {
                if (c is TabPage)
                {
                    c.BackColor = ThemeWhite;
                    c.ForeColor = ThemeTextPrimary;
                }
                else if (c is GroupBox)
                {
                    c.BackColor = ThemePanel;
                    c.ForeColor = ThemeTextPrimary;
                    c.Font = GetModernUIFont(9f, FontStyle.Bold);
                }
                else if (c is Panel || c is FlowLayoutPanel)
                {
                    c.BackColor = ThemeWhite;
                }
                else if (c is Label lbl)
                {
                    lbl.ForeColor = (lbl.Tag is string t && (t == "modifiedLbl")) ? ThemeTextSecondary : ThemeTextPrimary;
                    if (lbl.Tag is string t2 && t2 == "nameLbl")
                        lbl.Font = GetModernUIFont(10f, lbl.Font.Bold ? FontStyle.Bold : FontStyle.Regular);
                    else
                        lbl.Font = GetModernUIFont(lbl.Font.Size, lbl.Font.Style);
                }
                else if (c is Button btn)
                {
                    StyleButton(btn);
                }
                else if (c is TextBox tb)
                {
                    tb.BackColor = ThemeWhite;
                    tb.ForeColor = tb.ForeColor == SystemColors.GrayText ? SystemColors.GrayText : ThemeTextPrimary;
                    tb.BorderStyle = BorderStyle.FixedSingle;
                    tb.Font = GetModernUIFont(9f, FontStyle.Regular);
                }
                else if (c is ComboBox cb)
                {
                    cb.BackColor = ThemeWhite;
                    cb.ForeColor = ThemeTextPrimary;
                    cb.FlatStyle = FlatStyle.Flat;
                    cb.Font = GetModernUIFont(9f, FontStyle.Regular);
                }
                else if (c is ListBox lb)
                {
                    lb.BackColor = ThemeWhite;
                    lb.ForeColor = ThemeTextPrimary;
                    lb.BorderStyle = BorderStyle.FixedSingle;
                    lb.Font = GetModernUIFont(9f, FontStyle.Regular);
                }
                else if (c is TreeView tv)
                {
                    tv.BackColor = ThemeWhite;
                    tv.ForeColor = ThemeTextPrimary;
                    tv.BorderStyle = BorderStyle.FixedSingle;
                    tv.LineColor = Color.FromArgb(230, 230, 230);
                    tv.Font = GetModernUIFont(9f, FontStyle.Regular);
                }

                ApplyThemeRecursive(c);
            }
        }

        private void ApplyModernWhiteTheme()
        {
            BackColor = ThemeWhite;
            ForeColor = ThemeTextPrimary;
            Font = GetModernUIFont(9f, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterScreen;

            if (tabControl1 != null)
                tabControl1.Font = GetModernUIFont(9.5f, FontStyle.Regular);

            if (textForm != null)
                textForm.StartPosition = FormStartPosition.CenterParent;

            if (projectsPanel != null)
                projectsPanel.BackColor = ThemeWhite;

            if (tabPage0 != null) tabPage0.BackColor = ThemeWhite;
            if (tabPage4 != null) tabPage4.BackColor = ThemeWhite;

            ApplyThemeRecursive(this);
            if (projectsHeader != null)
            {
                projectsHeader.AutoSize = false;
                projectsHeader.Font = GetModernUIFont(10.5f, FontStyle.Bold);
                projectsHeader.TextAlign = ContentAlignment.MiddleLeft;
                projectsHeader.Height = 24;
            }
            if (levelTypeFilter != null)
            {
                levelTypeFilter.FlatStyle = FlatStyle.Standard;
                levelTypeFilter.BackColor = ThemeWhite;
                levelTypeFilter.ForeColor = ThemeTextPrimary;
            }
            if (previewModeComboBox != null)
            {
                previewModeComboBox.FlatStyle = FlatStyle.Standard;
                previewModeComboBox.BackColor = ThemeWhite;
                previewModeComboBox.ForeColor = ThemeTextPrimary;
            }
            if (levelListingSearchBox != null && searchBox != null)
            {
                levelListingSearchBox.BorderStyle = searchBox.BorderStyle;
                levelListingSearchBox.Font = searchBox.Font;
                levelListingSearchBox.TextAlign = searchBox.TextAlign;
            }
            EnsureProjectsPanelNoHorizontalScroll();
            UpdateRecentFilesPanel();
        }

        private void ShowOwnedForm(Form f)
        {
            if (f == null) return;
            f.StartPosition = FormStartPosition.CenterParent;
            f.Show(this);
        }
    }
}
