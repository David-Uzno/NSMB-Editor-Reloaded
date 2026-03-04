using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace NSMBe5
{
    public partial class LevelChooser
    {
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
                    MessageBox.Show("Patch created successfully!", "Success!");
                else
                    MessageBox.Show(end, "Something went wrong!", MessageBoxButtons.OK, MessageBoxIcon.Error);

                process.WaitForExit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unexpected exception has occured!\nDetails:\n" + ex, LanguageManager.Get("SpriteData", "ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
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
                    MessageBox.Show("ROM patched successfully, restarting NSMBe...");
                else
                    MessageBox.Show(end + "\n\nRestarting NSMBe!", "Something went wrong!", MessageBoxButtons.OK, MessageBoxIcon.Error);

                process.WaitForExit();

                Process process2 = new Process();
                process2.StartInfo.FileName = Application.ExecutablePath;
                process2.StartInfo.Arguments = "\"" + Properties.Settings.Default.ROMPath + "\"";
                process2.Start();
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unexpected exception has occured, restarting!\nDetails:\n" + ex, LanguageManager.Get("SpriteData", "ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);

                Process process2 = new Process();
                process2.StartInfo.FileName = Application.ExecutablePath;
                process2.StartInfo.Arguments = "\"" + Properties.Settings.Default.ROMPath + "\"";
                process2.Start();
                Application.Exit();
            }
        }
    }
}
