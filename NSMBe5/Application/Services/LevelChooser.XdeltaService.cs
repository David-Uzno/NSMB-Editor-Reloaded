using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace NSMBe5
{
    public partial class LevelChooser
    {
        private bool TryRunXdelta(string xdeltaExe, string arguments, out string errorOutput)
        {
            errorOutput = string.Empty;

            using (Process process = new Process())
            {
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.FileName = xdeltaExe;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.CreateNoWindow = false;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                string stdOut = process.StandardOutput.ReadToEnd();
                string stdErr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    errorOutput = string.IsNullOrWhiteSpace(stdErr) ? stdOut : stdErr;
                    if (string.IsNullOrWhiteSpace(errorOutput))
                    {
                        errorOutput = "xdelta3 terminó con código de salida " + process.ExitCode + ".";
                    }

                    return false;
                }

                if (!string.IsNullOrWhiteSpace(stdErr))
                {
                    errorOutput = stdErr;
                    return false;
                }

                return true;
            }
        }

        private void Xdelta_export_Click(object sender, EventArgs e)
        {
            string xdeltaExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Runtime", "Tooling", "xdelta3.exe");
            if (!File.Exists(xdeltaExe))
            {
                MessageBox.Show("Could not find xdelta3.exe in Runtime\\Tooling.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show("Please select a clean NSMB ROM file. After applying the patch, you will need to restart NSMBe.", "Export XDelta Patch", MessageBoxButtons.OK, MessageBoxIcon.Information);

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
                if (TryRunXdelta(xdeltaExe, str, out string errorOutput))
                {
                    MessageBox.Show("Patch created successfully!", "Success!");
                }
                else
                {
                    MessageBox.Show(errorOutput, "Something went wrong!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unexpected exception has occurred!\nDetails:\n" + ex, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Xdelta_import_Click(object sender, EventArgs e)
        {
            string xdeltaExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Runtime", "Tooling", "xdelta3.exe");
            if (!File.Exists(xdeltaExe))
            {
                MessageBox.Show("Could not find xdelta3.exe in Runtime\\Tooling.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DialogResult result = MessageBox.Show("All of the ROM contents will be replaced with the XDelta patch. Unlike NSMBe patches, this will overwrite the ROM entirely!\n\nYou will need to close and reopen NSMBe after the import has finished.\n\nDo you still want to continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.No)
                return;

            MessageBox.Show("Please select a clean NSMB ROM file.", "Import XDelta Patch", MessageBoxButtons.OK, MessageBoxIcon.Information);

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
                if (TryRunXdelta(xdeltaExe, str, out string errorOutput))
                {
                    MessageBox.Show("ROM patched successfully. Please close and reopen NSMBe to reload the ROM.", "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(errorOutput + "\n\nPlease close and reopen NSMBe before continuing.", "Something went wrong!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An unexpected exception has occurred!\nDetails:\n" + ex, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
