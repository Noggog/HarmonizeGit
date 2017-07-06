using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HarmonizeGit
{
    public class Settings
    {
        private static Settings _settings;
        public static Settings Instance => GetSettings();

        public bool AddMetadataToConfig = true;
        public bool Reroute = false;
        public bool ExportPathingConfigUpdates = true;
        public bool CheckForCircularConfigs = true;
        public bool Enabled = true;
        public bool TrackChildRepos = true;
        public bool Lock = true;

        Settings()
        {
        }

        private static Settings GetSettings()
        {
            if (_settings == null)
            {
                _settings = CreateSettings();
            }
            return _settings;
        }

        private static bool GetBool(XElement elem, string name, bool def)
        {
            var attr = elem.Element(name)?.Attribute("value");
            if (attr == null) return def;
            return bool.Parse(attr.Value);
        }

        private static Settings CreateSettings()
        {
            var assemb = System.Reflection.Assembly.GetEntryAssembly();
            if (assemb == null) return new Settings();
            var exe = new FileInfo(assemb.Location);
            FileInfo file = new FileInfo(exe.Directory + "/HarmonizeGitSettings.xml");
            if (!file.Exists) return new Settings();

            XDocument xml;
            using (var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            {
                using (var reader = new StreamReader(stream))
                {
                    xml = XDocument.Parse(reader.ReadToEnd());
                }
            }
            
            return new Settings()
            {
                AddMetadataToConfig = GetBool(xml.Root, nameof(AddMetadataToConfig), true),
                CheckForCircularConfigs = GetBool(xml.Root, nameof(CheckForCircularConfigs), true),
                Enabled = GetBool(xml.Root, nameof(Enabled), true),
                ExportPathingConfigUpdates = GetBool(xml.Root, nameof(ExportPathingConfigUpdates), true),
                Lock = GetBool(xml.Root, nameof(Lock), true),
                Reroute = GetBool(xml.Root, nameof(Reroute), false),
                TrackChildRepos = GetBool(xml.Root, nameof(TrackChildRepos), true),
            };
        }
    }
}
