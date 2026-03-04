using System;
using System.IO;
using FileSystemFile = NSMBe5.DSFileSystem.File;
using System.Windows.Forms;
using NSMBe5.DSFileSystem;
using NSMBe5.Editor;

namespace NSMBe5
{
    public partial class LevelChooser
    {
        private void EditSelectedLevel()
        {
            if (levelTreeView.SelectedNode == null)
                return;

            SetActiveLevelFromSelection();

            string editorCaption = "";
            if (levelTreeView.SelectedNode.Parent.Parent == null)
                editorCaption += levelTreeView.SelectedNode.Text;
            else
                editorCaption += levelTreeView.SelectedNode.Parent.Text + ", " + levelTreeView.SelectedNode.Text;

            try
            {
                LevelEditor newEditor = new LevelEditor(new NSMBLevel(new InternalLevelSource((string)levelTreeView.SelectedNode.Tag, editorCaption)));
                ShowOwnedForm(newEditor);
            }
            catch (AlreadyEditingException)
            {
                MessageBox.Show(LanguageManager.Get("Errors", "Level"));
            }
        }

        private void HexEditSelectedLevel()
        {
            if (levelTreeView.SelectedNode == null)
                return;

            SetActiveLevelFromSelection();

            string editorCaption = LanguageManager.Get("General", "EditingSomething") + " ";
            if (levelTreeView.SelectedNode.Parent.Parent == null)
                editorCaption += levelTreeView.SelectedNode.Text;
            else
                editorCaption += levelTreeView.SelectedNode.Parent.Text + ", " + levelTreeView.SelectedNode.Text;

            try
            {
                LevelHexEditor newEditor = new LevelHexEditor((string)levelTreeView.SelectedNode.Tag)
                {
                    Text = editorCaption
                };
                ShowOwnedForm(newEditor);
            }
            catch (AlreadyEditingException)
            {
                MessageBox.Show(LanguageManager.Get("Errors", "Level"));
            }
        }

        private void ImportSelectedLevel()
        {
            if (levelTreeView.SelectedNode == null)
                return;

            if (importLevelDialog.ShowDialog() == DialogResult.Cancel)
                return;

            string levelFilename = (string)levelTreeView.SelectedNode.Tag;
            FileSystemFile levelFile = ROM.getLevelFile(levelFilename);
            FileSystemFile bgFile = ROM.getBGDatFile(levelFilename);

            try
            {
                ExternalLevelSource level = new ExternalLevelSource(importLevelDialog.FileName);
                level.level.Import(levelFile, bgFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ExportSelectedLevel()
        {
            if (levelTreeView.SelectedNode == null)
                return;

            if (exportLevelDialog.ShowDialog() == DialogResult.Cancel)
                return;

            string levelFilename = (string)levelTreeView.SelectedNode.Tag;
            FileSystemFile levelFile = ROM.getLevelFile(levelFilename);
            FileSystemFile bgFile = ROM.getBGDatFile(levelFilename);

            FileStream fs = new FileStream(exportLevelDialog.FileName, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);
            new ExportedLevel(levelFile, bgFile).Write(bw);
            bw.Close();
        }

        private void OpenExternalLevel()
        {
            if (importLevelDialog.ShowDialog() == DialogResult.Cancel)
                return;

            try
            {
                ShowOwnedForm(new LevelEditor(new NSMBLevel(new ExternalLevelSource(importLevelDialog.FileName))));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ImportLevelFromClipboard()
        {
            if (MessageBox.Show(LanguageManager.Get("LevelChooser", "replaceclipboard"), LanguageManager.Get("LevelChooser", "replaceclipboardtitle"), MessageBoxButtons.YesNo) == DialogResult.No)
                return;

            try
            {
                string levelFilename = (string)levelTreeView.SelectedNode.Tag;
                FileSystemFile levelFile = ROM.getLevelFile(levelFilename);
                FileSystemFile bgFile = ROM.getBGDatFile(levelFilename);
                ClipboardLevelSource level = new ClipboardLevelSource();
                level.level.Import(levelFile, bgFile);
            }
            catch
            {
                MessageBox.Show(LanguageManager.Get("LevelChooser", "clipinvalidlevel"));
            }
        }

        private void ExportLevelToClipboard()
        {
            string levelFilename = (string)levelTreeView.SelectedNode.Tag;
            FileSystemFile levelFile = ROM.getLevelFile(levelFilename);
            FileSystemFile bgFile = ROM.getBGDatFile(levelFilename);

            ByteArrayInputStream strm = new ByteArrayInputStream(new byte[0]);
            BinaryWriter bw = new BinaryWriter(strm);

            new ExportedLevel(levelFile, bgFile).Write(bw);
            ClipboardLevelSource.copyData(strm.getData());
            bw.Close();
        }

        private void OpenLevelFromClipboard()
        {
            try
            {
                ShowOwnedForm(new LevelEditor(new NSMBLevel(new ClipboardLevelSource())));
            }
            catch
            {
                MessageBox.Show(LanguageManager.Get("LevelChooser", "clipinvalidlevel"));
            }
        }

        private void ShowDataFinder()
        {
            if (DataFinderForm == null || DataFinderForm.IsDisposed)
                DataFinderForm = new DataFinder();

            DataFinderForm.StartPosition = FormStartPosition.CenterParent;
            DataFinderForm.Show(this);
            DataFinderForm.Activate();
        }
    }
}
