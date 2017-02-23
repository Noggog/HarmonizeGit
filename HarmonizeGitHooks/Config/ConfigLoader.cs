using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace HarmonizeGitHooks
{
    public class ConfigLoader
    {
        public EventWaitHandle configSyncer = new EventWaitHandle(true, EventResetMode.AutoReset, "GIT_HARMONIZE_CONFIG_SYNCER");
        public EventWaitHandle gitIgnoreSyncer = new EventWaitHandle(true, EventResetMode.AutoReset, "GIT_HARMONIZE_GITIGNORE_SYNCER");
        public HarmonizeConfig OriginalConfig;
        public HarmonizeConfig Config;
        private HarmonizeGitBase harmonize;
        private Dictionary<string, HarmonizeConfig> configs = new Dictionary<string, HarmonizeConfig>();

        public HarmonizeConfig GetConfig(string path, bool forceReload = false)
        {
            HarmonizeConfig ret;
            if (configs.TryGetValue(path, out ret)) return ret;
            ret = LoadConfig(path);
            configs[path] = ret;
            return ret;
        }

        public void Init(HarmonizeGitBase harmonize)
        {
            this.harmonize = harmonize;
            this.Config = LoadConfig(HarmonizeGitBase.HarmonizeConfigPath);
            this.OriginalConfig = LoadConfig(HarmonizeGitBase.HarmonizeConfigPath);
        }
        
        private HarmonizeConfig LoadConfig(string path)
        {
            configSyncer.WaitOne();
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    var ret = HarmonizeConfig.Factory(stream);
                    if (ret.SetPathing(LoadPathing(HarmonizeGitBase.HarmonizePathingPath)))
                    {
                        this.UpdatePathingConfig(trim: false);
                    }
                    return ret;
                }
            }
            finally
            {
                configSyncer.Set();
            }
        }

        private PathingConfig LoadPathing(string path)
        {
            FileInfo file = new FileInfo(path);
            if (!file.Exists) return new PathingConfig();

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                return PathingConfig.Factory(stream);
            }
        }

        private void AddPathingToGitIgnore()
        {
            if (!Properties.Settings.Default.AddPathingToGitIgnore) return;
            gitIgnoreSyncer.WaitOne();
            try
            {
                FileInfo file = new FileInfo(HarmonizeGitBase.GitIgnorePath);
                if (file.Exists)
                {
                    var lines = File.ReadAllLines(HarmonizeGitBase.GitIgnorePath).ToList();
                    foreach (var line in lines)
                    {
                        if (line.Trim().Equals(HarmonizeGitBase.HarmonizePathingPath)) return;
                    }
                    lines.Add(HarmonizeGitBase.HarmonizePathingPath);
                    File.WriteAllLines(HarmonizeGitBase.GitIgnorePath, lines);
                }
                else
                {
                    File.WriteAllText(HarmonizeGitBase.GitIgnorePath, HarmonizeGitBase.HarmonizePathingPath);
                }
            }
            finally
            {
                gitIgnoreSyncer.Set();
            }
        }

        public void WriteConfig(HarmonizeConfig config)
        {
            List<RepoListing> changed = new List<RepoListing>();
            foreach (var listing in this.Config.ParentRepos)
            {
                using (var repo = new Repository(listing.Path))
                {
                    if (object.Equals(listing.Sha, repo.Head.Tip.Sha)) continue;
                    changed.Add(listing);
                    listing.SetToCommit(repo.Head.Tip);
                }
            }

            if (changed.Count > 0
                || this.Config.Equals(this.OriginalConfig)) return;

            this.harmonize.WriteLine("Updating config as parent repos have changed: ");
            foreach (var change in changed)
            {
                this.harmonize.WriteLine("  " + change.Nickname);
            }

            string xmlStr;
            XmlSerializer xsSubmit = new XmlSerializer(typeof(HarmonizeConfig));
            var settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.OmitXmlDeclaration = true;
            var emptyNs = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            using (var sw = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(sw, settings))
                {
                    xsSubmit.Serialize(writer, this.Config, emptyNs);
                    xmlStr = sw.ToString();
                }
            }

            configSyncer.WaitOne();
            try
            {
                File.WriteAllText(HarmonizeGitBase.HarmonizeConfigPath, xmlStr);
            }
            finally
            {
                configSyncer.Set();
            }
        }

        public void UpdatePathingConfig(bool trim)
        {
            if (!Properties.Settings.Default.ExportPathingConfigUpdates) return;

            if (trim)
            {
                foreach (var path in this.Config.Pathing.Paths.ToList())
                {
                    if (!this.Config.ParentRepos.Any((listing) => listing.Nickname.Equals(path.Nickname)))
                    {
                        this.Config.Pathing.Paths.Remove(path);
                    }
                }
            }

            if (this.Config.Pathing.Equals(this.OriginalConfig.Pathing)) return;

            string xmlStr;
            XmlSerializer xsSubmit = new XmlSerializer(typeof(PathingConfig));
            var settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.OmitXmlDeclaration = true;
            var emptyNs = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            using (var sw = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(sw, settings))
                {
                    xsSubmit.Serialize(writer, this.Config.Pathing, emptyNs);
                    xmlStr = sw.ToString();
                }
            }

            bool created;
            configSyncer.WaitOne();
            try
            {
                FileInfo file = new FileInfo(HarmonizeGitBase.HarmonizePathingPath);
                created = !file.Exists;
                File.WriteAllText(HarmonizeGitBase.HarmonizePathingPath, xmlStr);
            }
            finally
            {
                configSyncer.Set();
            }

            if (created)
            {
                AddPathingToGitIgnore();
            }
        }
    }
}
