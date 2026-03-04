using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NSMBe5.Editor;

namespace NSMBe5
{
    public partial class LevelChooser
    {
        private readonly Dictionary<string, Image> levelPreviewCache = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Image> levelAdvancedPreviewCache = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Image> levelAdvancedReducedPreviewCache = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);

        private void ClearPreviewCaches()
        {
            levelPreviewCache.Clear();
            levelAdvancedPreviewCache.Clear();
            levelAdvancedReducedPreviewCache.Clear();
        }

        private void UpdateLevelPreview(string internalName)
        {
            if (levelPreviewPicture == null)
                return;

            LevelListingPreviewMode mode = GetCurrentPreviewMode();
            if (mode == LevelListingPreviewMode.None)
            {
                levelPreviewName.Text = "";
                levelPreviewMeta.Text = "";
                levelPreviewAdvancedPicture.Image = null;
                levelPreviewAdvancedPicture.Visible = false;
                levelPreviewPicture.Image = null;
                levelPreviewPicture.Visible = false;
                return;
            }

            if (string.IsNullOrEmpty(internalName))
            {
                levelPreviewName.Text = "";
                levelPreviewMeta.Text = "";
                levelPreviewAdvancedPicture.Image = null;
                levelPreviewAdvancedPicture.Visible = false;
                levelPreviewPicture.Image = null;
                levelPreviewPicture.Visible = false;
                LayoutModernLevelListingUI();
                return;
            }

            LevelListingItem item = allLevelListingItems.FirstOrDefault(x => string.Equals(x.InternalName, internalName, StringComparison.OrdinalIgnoreCase));
            if (item == null)
                return;

            if (mode == LevelListingPreviewMode.Advanced)
            {
                Image advancedPreviewImage = null;
                if (!levelAdvancedPreviewCache.TryGetValue(internalName, out advancedPreviewImage) || advancedPreviewImage == null)
                {
                    advancedPreviewImage = GenerateAdvancedPreviewImage(item);
                    if (advancedPreviewImage != null)
                        levelAdvancedPreviewCache[internalName] = advancedPreviewImage;
                    else
                        levelAdvancedPreviewCache.Remove(internalName);
                }

                levelPreviewAdvancedPicture.Image = advancedPreviewImage;
                levelPreviewAdvancedPicture.Visible = advancedPreviewImage != null;
                levelPreviewPicture.Visible = false;
            }
            else if (mode == LevelListingPreviewMode.AdvancedReduced)
            {
                Image reducedPreviewImage = null;
                if (!levelAdvancedReducedPreviewCache.TryGetValue(internalName, out reducedPreviewImage) || reducedPreviewImage == null)
                {
                    reducedPreviewImage = GenerateAdvancedReducedPreviewImage(item);
                    if (reducedPreviewImage != null)
                        levelAdvancedReducedPreviewCache[internalName] = reducedPreviewImage;
                    else
                        levelAdvancedReducedPreviewCache.Remove(internalName);
                }

                levelPreviewAdvancedPicture.Image = reducedPreviewImage;
                levelPreviewAdvancedPicture.Visible = reducedPreviewImage != null;
                levelPreviewPicture.Visible = false;
            }
            else
            {
                Image previewImage = null;
                if (!levelPreviewCache.TryGetValue(internalName, out previewImage) || previewImage == null)
                {
                    previewImage = GeneratePreviewImage(item);
                    if (previewImage != null)
                        levelPreviewCache[internalName] = previewImage;
                    else
                        levelPreviewCache.Remove(internalName);
                }

                levelPreviewPicture.Image = previewImage;
                levelPreviewPicture.Visible = previewImage != null;
                levelPreviewAdvancedPicture.Visible = false;
            }

            levelPreviewName.Text = item.DisplayName;
            levelPreviewMeta.Text =
                GetLevelChooserText("LevelListingPreviewMetaWorld", "World:") + " " + item.World + Environment.NewLine +
                GetLevelChooserText("LevelListingPreviewMetaName", "Name:") + " " + item.InternalName;
            LayoutModernLevelListingUI();
        }

        private Image GenerateAdvancedPreviewImage(LevelListingItem item)
        {
            NSMBLevel level = null;
            try
            {
                level = new NSMBLevel(new InternalLevelSource(item.InternalName, item.DisplayName ?? item.InternalName));

                using (LevelEditorControl previewControl = new LevelEditorControl())
                {
                    previewControl.Size = new Size(900, 650);
                    previewControl.CreateControl();
                    previewControl.Initialise(level.GFX, level, null);
                    previewControl.updateTileCache(true);
                    previewControl.repaint();
                    Application.DoEvents();
                    return previewControl.CreateFullLevelPreview(192, 108);
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                try { if (level != null) level.close(); } catch { }
            }
        }

        private Image GenerateAdvancedReducedPreviewImage(LevelListingItem item)
        {
            NSMBLevel level = null;
            try
            {
                level = new NSMBLevel(new InternalLevelSource(item.InternalName, item.DisplayName ?? item.InternalName));

                using (LevelEditorControl previewControl = new LevelEditorControl())
                {
                    previewControl.Size = new Size(900, 650);
                    previewControl.CreateControl();
                    previewControl.Initialise(level.GFX, level, null);
                    previewControl.updateTileCache(true);
                    previewControl.repaint();
                    Application.DoEvents();
                    return previewControl.CreateViewportPreview(192, 108);
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                try { if (level != null) level.close(); } catch { }
            }
        }

        private Image GeneratePreviewImage(LevelListingItem item)
        {
            int width = 192;
            int height = 108;
            Bitmap bmp = new Bitmap(width, height);

            try
            {
                NSMBLevel level = new NSMBLevel(new InternalLevelSource(item.InternalName, item.DisplayName ?? item.InternalName));

                RectangleF contentBounds = RectangleF.Empty;
                bool hasContent = false;

                Action<RectangleF> includeRect = rect =>
                {
                    if (rect.Width <= 0 || rect.Height <= 0)
                        return;

                    if (!hasContent)
                    {
                        contentBounds = rect;
                        hasContent = true;
                    }
                    else
                    {
                        contentBounds = RectangleF.Union(contentBounds, rect);
                    }
                };

                foreach (NSMBTile obj in level.Objects)
                    includeRect(new RectangleF(obj.X, obj.Y, Math.Max(1, obj.Width), Math.Max(1, obj.Height)));

                foreach (NSMBStageObj sprite in level.Sprites)
                {
                    Rectangle r = sprite.GetMinimapBounds();
                    includeRect(new RectangleF(r.X, r.Y, Math.Max(1, r.Width), Math.Max(1, r.Height)));
                }

                foreach (NSMBView view in level.Views)
                    includeRect(new RectangleF(view.X / 16f, view.Y / 16f, Math.Max(1, view.Width / 16f), Math.Max(1, view.Height / 16f)));

                foreach (NSMBView zone in level.Zones)
                    includeRect(new RectangleF(zone.X / 16f, zone.Y / 16f, Math.Max(1, zone.Width / 16f), Math.Max(1, zone.Height / 16f)));

                if (!hasContent)
                    contentBounds = new RectangleF(0, 0, 32, 16);

                float marginWorld = 2f;
                contentBounds = RectangleF.FromLTRB(
                    contentBounds.Left - marginWorld,
                    contentBounds.Top - marginWorld,
                    contentBounds.Right + marginWorld,
                    contentBounds.Bottom + marginWorld);

                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.Clear(Color.FromArgb(47, 79, 79));

                    float pad = 4f;
                    float availW = Math.Max(1f, width - (pad * 2f));
                    float availH = Math.Max(1f, height - (pad * 2f));
                    float scaleX = availW / Math.Max(1f, contentBounds.Width);
                    float scaleY = availH / Math.Max(1f, contentBounds.Height);
                    float scale = Math.Min(scaleX, scaleY);

                    float renderW = contentBounds.Width * scale;
                    float renderH = contentBounds.Height * scale;
                    float offsetX = pad + (availW - renderW) / 2f;
                    float offsetY = pad + (availH - renderH) / 2f;

                    RectangleF ToScreen(RectangleF worldRect)
                    {
                        return new RectangleF(
                            offsetX + (worldRect.X - contentBounds.X) * scale,
                            offsetY + (worldRect.Y - contentBounds.Y) * scale,
                            Math.Max(1f, worldRect.Width * scale),
                            Math.Max(1f, worldRect.Height * scale));
                    }

                    using (SolidBrush background = new SolidBrush(Color.FromArgb(60, 70, 70, 70)))
                        g.FillRectangle(background, offsetX, offsetY, renderW, renderH);

                    using (Pen framePen = new Pen(Color.FromArgb(120, 180, 180, 180), 1f))
                        g.DrawRectangle(framePen, offsetX, offsetY, renderW, renderH);

                    foreach (NSMBTile obj in level.Objects)
                    {
                        RectangleF rect = ToScreen(new RectangleF(obj.X, obj.Y, Math.Max(1, obj.Width), Math.Max(1, obj.Height)));
                        Brush brush = (obj.TileID == 0 && obj.Tileset == 0)
                            ? Brushes.SlateGray
                            : Brushes.White;
                        g.FillRectangle(brush, rect);
                    }

                    foreach (NSMBStageObj sprite in level.Sprites)
                    {
                        Rectangle r = sprite.GetMinimapBounds();
                        RectangleF rect = ToScreen(new RectangleF(r.X, r.Y, Math.Max(1, r.Width), Math.Max(1, r.Height)));
                        g.FillRectangle(Brushes.Chartreuse, rect);
                    }

                    using (Pen viewPen = new Pen(Color.LightSteelBlue, 1f))
                    using (Pen zonePen = new Pen(Color.PaleGreen, 1f))
                    {
                        foreach (NSMBView view in level.Views)
                        {
                            RectangleF rect = ToScreen(new RectangleF(view.X / 16f, view.Y / 16f, Math.Max(1, view.Width / 16f), Math.Max(1, view.Height / 16f)));
                            g.DrawRectangle(viewPen, rect.X, rect.Y, rect.Width, rect.Height);
                        }

                        foreach (NSMBView zone in level.Zones)
                        {
                            RectangleF rect = ToScreen(new RectangleF(zone.X / 16f, zone.Y / 16f, Math.Max(1, zone.Width / 16f), Math.Max(1, zone.Height / 16f)));
                            g.DrawRectangle(zonePen, rect.X, rect.Y, rect.Width, rect.Height);
                        }
                    }
                }
            }
            catch
            {
                bmp.Dispose();
                return null;
            }

            return bmp;
        }
    }
}
