using System;
using System.Windows.Forms;
using NSMBe5.Patcher;

namespace NSMBe5
{
    public partial class LevelChooser
    {
        private CodePatcher GetCodePatcher(System.IO.DirectoryInfo path)
        {
            switch (Properties.Settings.Default.CodePatchingMethod)
            {
                case CodePatcher.Method.NSMBe:
                    return new CodePatcherNSMBe(path);
                case CodePatcher.Method.Fireflower:
                    return new CodePatcherModern(path, "fireflower");
                case CodePatcher.Method.NCPatcher:
                    return new CodePatcherModern(path, "ncpatcher");
                default:
                    throw new Exception("Illegal code patching method specified");
            }
        }

        private void CompileInsert_Click(object sender, EventArgs e)
        {
            CodePatcher patcher = GetCodePatcher(ROM.romfile.Directory);

            try
            {
                patcher.Execute();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong during code patching:\n" + ex.Message, "Code patching", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CleanBuild_Click(object sender, EventArgs e)
        {
            CodePatcher patcher = GetCodePatcher(ROM.romfile.Directory);

            try
            {
                patcher.CleanBuild();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Something went wrong during build cleaning:\n" + ex.Message, "Build cleaning", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
