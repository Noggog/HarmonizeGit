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
                if (HarmonizeFunctionality.TryLoadConfig(
                    path,
                    this.harmonize.RepoLoader,
                    out var config))
                {
                    return config;
                }
                return null;
            }
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
