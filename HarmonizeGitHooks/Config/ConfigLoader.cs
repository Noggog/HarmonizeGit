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
        public HarmonizeConfig Config;
        private HarmonizeGitBase harmonize;
        private Dictionary<string, HarmonizeConfig> configs = new Dictionary<string, HarmonizeConfig>();
        private Dictionary<string, PathingConfig> pathingConfigs = new Dictionary<string, PathingConfig>();

        public void Init(HarmonizeGitBase harmonize)
        {
            this.harmonize = harmonize;
            this.Config = GetConfig(".");
            if (this.Config == null) return;
            this.UpdatePathingConfig(this.Config, trim: false);
        }

        #region Config
        public HarmonizeConfig GetConfig(string path)
        {
            path = path.Trim();
            HarmonizeConfig ret;
            if (configs.TryGetValue(path, out ret)) return ret;
            ret = LoadConfig(path);
            configs[path] = ret;
            return ret;
        }

        private HarmonizeConfig LoadConfig(string path)
        {
            configSyncer.WaitOne();
            try
            {
                this.harmonize.WriteLine($"Loading config at path {path}");
                FileInfo file = new FileInfo(path + "/" + HarmonizeGitBase.HarmonizeConfigPath);
                if (!file.Exists) return null;
                var pathing = LoadPathing(path);
                using (var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
                {
                    return HarmonizeConfig.Factory(
                        this.harmonize,
                        path,
                        stream,
                        pathing);
                }
            }
            finally
            {
                configSyncer.Set();
            }
        }

        public void WriteConfig(HarmonizeConfig config, string path)
        {
            List<RepoListing> changed = new List<RepoListing>();
            foreach (var listing in config.ParentRepos)
            {
                this.harmonize.WriteLine($"Checking for sha changes {listing.Nickname} at path {listing.Path}.");
                using (var repo = new Repository(listing.Path))
                {
                    this.harmonize.WriteLine($"Config sha {listing.Sha} compared to current sha {repo.Head.Tip.Sha}.");
                    if (object.Equals(listing.Sha, repo.Head.Tip.Sha)) continue;
                    changed.Add(listing);
                    listing.SetToCommit(repo.Head.Tip);
                    this.harmonize.WriteLine($"Changed to sha {repo.Head.Tip.Sha}.");
                }
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
                    xsSubmit.Serialize(writer, config, emptyNs);
                    xmlStr = sw.ToString();
                }
            }

            if (changed.Count == 0
                && object.Equals(config.OriginalXML, xmlStr)) return;

            path = path + "/" + HarmonizeGitBase.HarmonizeConfigPath;

            this.harmonize.WriteLine($"Updating config at {path}");
            if (changed.Count > 0)
            {
                this.harmonize.WriteLine("Parent repos have changed: ");
                foreach (var change in changed)
                {
                    this.harmonize.WriteLine("  " + change.Nickname);
                }
            }

            configSyncer.WaitOne();
            try
            {
                File.WriteAllText(path, xmlStr);
            }
            finally
            {
                configSyncer.Set();
            }
        }
        #endregion

        #region Pathing
        public PathingConfig GetPathing(string path)
        {
            path = path.Trim();
            PathingConfig ret;
            if (pathingConfigs.TryGetValue(path, out ret)) return ret;
            ret = LoadPathing(path);
            pathingConfigs[path] = ret;
            return ret;
        }

        private PathingConfig LoadPathing(string path)
        {
            FileInfo file = new FileInfo(path + "/" + HarmonizeGitBase.HarmonizePathingPath);
            if (!file.Exists)
            {
                return new PathingConfig();
            }

            using (var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            {
                return PathingConfig.Factory(stream);
            }
        }

        private bool LoadPathing(string path, out PathingConfig config)
        {
            FileInfo file = new FileInfo(path + "/" + HarmonizeGitBase.HarmonizePathingPath);
            if (!file.Exists)
            {
                config = null;
                return false;
            }

            using (var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            {
                config = PathingConfig.Factory(stream);
                return true;
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

        public void UpdatePathingConfig(HarmonizeConfig config, bool trim)
        {
            if (!Properties.Settings.Default.ExportPathingConfigUpdates) return;

            if (trim)
            {
                foreach (var path in config.Pathing.Paths.ToList())
                {
                    if (!config.ParentRepos.Any((listing) => listing.Nickname.Equals(path.Nickname)))
                    {
                        config.Pathing.Paths.Remove(path);
                    }
                }
            }

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
                    xsSubmit.Serialize(writer, config.Pathing, emptyNs);
                    xmlStr = sw.ToString();
                }
            }

            if (object.Equals(config.Pathing.OriginalXML, xmlStr)) return;

            this.harmonize.WriteLine("Writing pathing update");

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
        #endregion
    }
}
