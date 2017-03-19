﻿using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace HarmonizeGit
{
    public class ConfigLoader
    {
        public HarmonizeConfig Config;
        private HarmonizeGitBase harmonize;
        private Dictionary<string, HarmonizeConfig> configs = new Dictionary<string, HarmonizeConfig>();
        private Dictionary<Tuple<string, string>, HarmonizeConfig> repoConfigs = new Dictionary<Tuple<string, string>, HarmonizeConfig>();
        private Dictionary<string, PathingConfig> pathingConfigs = new Dictionary<string, PathingConfig>();

        public void Init(HarmonizeGitBase harmonize)
        {
            this.harmonize = harmonize;
            this.Config = GetConfig(harmonize.TargetPath);
            if (this.Config == null) return;
            this.UpdatePathingConfig(this.Config);
        }

        #region Config
        public HarmonizeConfig GetConfig(string path)
        {
            path = path.Trim();
            if (configs.TryGetValue(path, out HarmonizeConfig ret)) return ret;
            ret = LoadConfig(path);
            configs[path] = ret;
            return ret;
        }

        public HarmonizeConfig GetConfigFromRepo(
            Repository repo,
            Commit commit)
        {
            var tuple = new Tuple<string, string>(
                repo.Info.WorkingDirectory,
                commit.Sha);
            if (repoConfigs.TryGetValue(tuple, out HarmonizeConfig ret)) return ret;
            this.harmonize.WriteLine($"Loading config from repo at path {repo.Info.WorkingDirectory} at commit {commit.Sha} ");
            ret = HarmonizeConfig.Factory(
                this.harmonize,
                repo.Info.WorkingDirectory,
                commit);
            repoConfigs[tuple] = ret;
            return ret;
        }

        private HarmonizeConfig LoadConfig(string path)
        {
            using (this.harmonize.LockManager.GetLock(LockType.Harmonize, path))
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
        }

        public void WriteSyncAndConfig(HarmonizeConfig config, string path)
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

            if (WriteConfig(config, path))
            {
                if (changed.Count > 0)
                {
                    this.harmonize.WriteLine("Parent repos have changed: ");
                    foreach (var change in changed)
                    {
                        this.harmonize.WriteLine("  " + change.Nickname);
                    }
                }
            }
        }

        private bool WriteConfig(HarmonizeConfig config, string path)
        {
            string xmlStr;
            XmlSerializer xsSubmit = new XmlSerializer(typeof(HarmonizeConfig));
            var settings = new XmlWriterSettings()
            {
                Indent = true,
                OmitXmlDeclaration = true
            };
            var emptyNs = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            using (var sw = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(sw, settings))
                {
                    xsSubmit.Serialize(writer, config, emptyNs);
                    xmlStr = sw.ToString();
                }
            }

            if (object.Equals(config.OriginalXML, xmlStr)) return false;

            path = path + "/" + HarmonizeGitBase.HarmonizeConfigPath;

            this.harmonize.WriteLine($"Updating config at {path}");

            using (this.harmonize.LockManager.GetLock(LockType.Harmonize, path))
            {
                File.WriteAllText(path, xmlStr);
            }
            return true;
        }
        #endregion

        #region Pathing
        public PathingConfig GetPathing(string path)
        {
            path = path.Trim();
            if (pathingConfigs.TryGetValue(path, out PathingConfig ret)) return ret;
            ret = LoadPathing(path);
            pathingConfigs[path] = ret;
            return ret;
        }

        private PathingConfig LoadPathing(string path)
        {
            using (this.harmonize.LockManager.GetLock(LockType.Pathing, path))
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
                foreach (var listing in config.Paths)
                {
                    harmonize.WriteLine($"{listing.Nickname} set to path {listing.Path}.");
                }
                return true;
            }
        }

        private void AddToGitIgnore(
            string path,
            string toAdd)
        {
            path = path + "/" + HarmonizeGitBase.GitIgnorePath;

            using (this.harmonize.LockManager.GetLock(LockType.GitIgnore, path))
            {
                FileInfo file = new FileInfo(path);
                if (file.Exists)
                {
                    var lines = File.ReadAllLines(file.FullName).ToList();
                    foreach (var line in lines)
                    {
                        if (line.Trim().Equals(toAdd)) return;
                    }
                    lines.Add(toAdd);
                    File.WriteAllLines(path, lines);
                }
                else
                {
                    File.WriteAllText(path, toAdd);
                }
            }
        }

        public void UpdatePathingConfig(HarmonizeConfig config)
        {
            if (!Properties.Settings.Default.ExportPathingConfigUpdates) return;

            string xmlStr;
            XmlSerializer xsSubmit = new XmlSerializer(typeof(PathingConfig));
            var settings = new XmlWriterSettings()
            {
                Indent = true,
                OmitXmlDeclaration = true
            };
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

            using (this.harmonize.LockManager.GetLock(LockType.Pathing, this.harmonize.TargetPath))
            {
                File.WriteAllText(HarmonizeGitBase.HarmonizePathingPath, xmlStr);
            }
        }
        #endregion
    }
}