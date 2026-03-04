using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NSMBe5
{
    internal interface IRecentFilesStore
    {
        List<string> LoadExisting();
        void Save(List<string> files);
    }

    internal interface IProjectDisplayNameStore
    {
        Dictionary<string, string> Load(string path);
        void Save(string path, Dictionary<string, string> displayNames);
    }

    internal sealed class SettingsRecentFilesStore : IRecentFilesStore
    {
        public List<string> LoadExisting()
        {
            var recentFiles = new List<string>();
            if (string.IsNullOrEmpty(Properties.Settings.Default.RecentFiles))
                return recentFiles;

            var files = Properties.Settings.Default.RecentFiles.Split(';');
            foreach (var file in files)
            {
                if (!string.IsNullOrEmpty(file) && File.Exists(file))
                    recentFiles.Add(file);
            }
            return recentFiles;
        }

        public void Save(List<string> files)
        {
            Properties.Settings.Default.RecentFiles = string.Join(";", files.Where(x => !string.IsNullOrEmpty(x)).ToArray());
            Properties.Settings.Default.Save();
        }
    }

    internal sealed class FileProjectDisplayNameStore : IProjectDisplayNameStore
    {
        public Dictionary<string, string> Load(string path)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(path)) return result;
                foreach (var line in File.ReadAllLines(path))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    int idx = line.IndexOf('\t');
                    if (idx <= 0) continue;
                    string p = line.Substring(0, idx);
                    string n = line.Substring(idx + 1);
                    result[p] = n;
                }
            }
            catch
            {
            }
            return result;
        }

        public void Save(string path, Dictionary<string, string> displayNames)
        {
            try
            {
                var lines = new List<string>();
                foreach (var kv in displayNames)
                {
                    string safeName = kv.Value?.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ') ?? "";
                    lines.Add(kv.Key + "\t" + safeName);
                }
                File.WriteAllLines(path, lines.ToArray());
            }
            catch
            {
            }
        }
    }
}
