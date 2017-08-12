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

namespace HarmonizeGit
{
    public class ConfigLoader
    {
        public HarmonizeConfig Config;
        private HarmonizeGitBase harmonize;
        private Dictionary<string, HarmonizeConfig> configs = new Dictionary<string, HarmonizeConfig>();
        private Dictionary<RepoConfigKey, HarmonizeConfig> repoConfigs = new Dictionary<RepoConfigKey, HarmonizeConfig>();
        private Dictionary<string, PathingConfig> pathingConfigs = new Dictionary<string, PathingConfig>();

        public void Init(HarmonizeGitBase harmonize)
        {
            this.harmonize = harmonize;
            this.Config = GetConfig(harmonize.TargetPath);
            if (this.Config == null
                || this.Config.IsMidMerge) return;
            this.Config.Pathing.WriteToPath(harmonize.TargetPath);
        }

        #region Config
        public HarmonizeConfig GetConfig(string path, bool force = false)
        {
            path = path.Trim();
            if (!force && configs.TryGetValue(path, out HarmonizeConfig ret)) return ret;
            ret = LoadConfig(path);
            configs[path] = ret;
            return ret;
        }

        public HarmonizeConfig GetConfigFromRepo(
            Repository repo,
            Commit commit)
        {
            var key = new RepoConfigKey()
            {
                WorkingDir = repo.Info.WorkingDirectory,
                CommitSha = commit.Sha
            };
            if (repoConfigs.TryGetValue(key, out HarmonizeConfig ret)) return ret;
            this.harmonize.Logger.WriteLine($"Loading config from repo at path {repo.Info.WorkingDirectory} at commit {commit.Sha} ");
            ret = HarmonizeConfig.Factory(
                this.harmonize,
                repo.Info.WorkingDirectory,
                commit);
            repoConfigs[key] = ret;
            return ret;
        }

        private HarmonizeConfig LoadConfig(string path)
        {
            using (LockManager.GetLock(LockType.Harmonize, path))
            {
                this.harmonize.Logger.WriteLine($"Loading config at path {path}");
                FileInfo file = new FileInfo(path + "/" + Constants.HarmonizeConfigPath);
                if (!file.Exists) return null;
                var pathing = PathingConfig.Factory(path);
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

        public async Task SyncAndWriteConfig(HarmonizeConfig config, string path)
        {
            List<RepoListing> changed = new List<RepoListing>();
            changed.AddRange((await Task.WhenAll(config.ParentRepos.Select(
                (listing) =>
                {
                    return Task.Run(() =>
                    {
                        this.harmonize.Logger.WriteLine($"Checking for sha changes {listing.Nickname} at path {listing.Path}.");
                        var repo = this.harmonize.RepoLoader.GetRepo(listing.Path);
                        this.harmonize.Logger.WriteLine($"Config sha {listing.Sha} compared to current sha {repo.Head.Tip.Sha}.");
                        if (object.Equals(listing.Sha, repo.Head.Tip.Sha)) return null;
                        listing.SetToCommit(repo.Head.Tip);
                        this.harmonize.Logger.WriteLine($"Changed to sha {repo.Head.Tip.Sha}.");
                        return listing;
                    });
                })))
                .Where((listing) => listing != null));

            if (WriteConfig(config, path))
            {
                if (changed.Count > 0)
                {
                    this.harmonize.Logger.WriteLine("Parent repos have changed: ");
                    foreach (var change in changed)
                    {
                        this.harmonize.Logger.WriteLine("  " + change.Nickname);
                    }
                }
            }
        }

        public bool WriteConfig(HarmonizeConfig config, string path)
        {
            if (object.Equals(config, config?.OriginalConfig)) return false;

            path = path + "/" + Constants.HarmonizeConfigPath;
            this.harmonize.Logger.WriteLine($"Updating config at {path}");

            using (LockManager.GetLock(LockType.Harmonize, path))
            {
                config.WriteToPath(path);
            }
            return true;
        }
        #endregion

        #region Pathing
        public PathingConfig GetPathing(string path)
        {
            path = path.Trim();
            if (pathingConfigs.TryGetValue(path, out PathingConfig ret)) return ret;
            ret = PathingConfig.Factory(path);
            pathingConfigs[path] = ret;
            return ret;
        }

        private void AddToGitIgnore(
            string path,
            string toAdd)
        {
            path = path + "/" + Constants.GitIgnorePath;

            using (LockManager.GetLock(LockType.GitIgnore, path))
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
        #endregion
    }
}
