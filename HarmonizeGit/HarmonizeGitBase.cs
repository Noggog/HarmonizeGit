﻿using FishingWithGit;
using FishingWithGit.Common;
using LibGit2Sharp;
using Noggog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace HarmonizeGit
{
    public class HarmonizeGitBase : IDisposable
    {
        private bool _disposed;
        public ConfigLoader ConfigLoader { get; private set; }
        public ChildrenLoader ChildLoader { get; private set; }
        private readonly Lazy<RepoLoader> _RepoLoader;
        public RepoLoader RepoLoader => _RepoLoader.Value;
        public readonly string TargetPath;
        public string ConfigPath => Path.Combine(TargetPath, Constants.HarmonizeConfigPath);
        public HarmonizeConfig Config;
        public bool Silent;
        public bool FileLock;
        public TypicalLogger Logger;
        public Repository Repo => this.RepoLoader.GetRepo(this.TargetPath);

        public HarmonizeGitBase(string targetPath)
        {
            this.TargetPath = targetPath;
            this._RepoLoader = new Lazy<RepoLoader>(() => new RepoLoader(this.TargetPath));
        }

        public async Task<bool> Handle(string[] args)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            try
            {
                return await Handle_Internal(args);
            }
            catch (Exception)
            {
                if (!this.Logger?.ShouldLogToFile ?? false)
                {
                    this.Logger.ActivateAndFlushLogging();
                }
                throw;
            }
            finally
            {
                if (this._RepoLoader.IsValueCreated)
                {
                    this._RepoLoader.Value.Dispose();
                }
                if (this.Logger?.ShouldLogToFile ?? false)
                {
                    this.Logger.LogResults(sw, "HarmonizeGit");
                }
            }
        }

        private async Task<bool> Handle_Internal(string[] args)
        {
            if (args.Length < 2) return true;

            if (!HookTypeExt.TryGetHook(args[0], out HookType hookType)) return true;

            List<string> trimmedArgs = new List<string>();
            for (int i = 2; i < args.Length; i++)
            {
                trimmedArgs.Add(args[i]);
            }
            args = trimmedArgs.ToArray();

            TypicalHandlerBase handler;
            switch (hookType)
            {
                case HookType.Pre_Checkout:
                    handler = new PreCheckoutHandler(this, new CheckoutArgs(args));
                    break;
                case HookType.Pre_Reset:
                    handler = new PreResetHandler(this, new ResetArgs(args));
                    break;
                case HookType.Post_Reset:
                    handler = new PostResetHandler(this, new ResetArgs(args));
                    break;
                case HookType.Pre_Commit:
                    handler = new PreCommitHandler(this, new CommitArgs(args));
                    break;
                case HookType.Post_Commit:
                case HookType.Post_CherryPick:
                    handler = new PostCommitHandler(this, new CommitArgs(args));
                    break;
                case HookType.Post_Merge:
                    handler = new PostMergeHandler(this, new MergeArgs(args));
                    break;
                case HookType.Post_Status:
                    handler = new PostStatusHandler(this, new StatusArgs(args));
                    break;
                case HookType.Post_Take:
                    handler = new PostTakeHandler(this, new TakeArgs(args));
                    break;
                case HookType.Pre_Rebase:
                    handler = new PreRebaseHandler(this, new RebaseArgs(args));
                    break;
                case HookType.Post_Rebase_Continue:
                case HookType.Post_Rebase:
                    handler = new PostRebaseHandler(this, new RebaseInProgressArgs(args));
                    break;
                case HookType.Post_Pull:
                    handler = new PostPullHandler(this, new PullArgs(args));
                    break;
                case HookType.Pre_Branch:
                    handler = new PreBranchHandler(this, new BranchArgs(args));
                    break;
                case HookType.Pre_Pull:
                    handler = new PrePullHandler(this, new PullArgs(args));
                    break;
                case HookType.Pre_Push:
                    handler = new PrePushHandler(this, new PushArgs(args));
                    break;
                default:
                    return true;
            }

            this.Silent = handler.Args.Silent;

            Init();
            if (this.Config == null)
            {
                if (handler.NeedsConfig)
                {
                    this.Logger.WriteLine("No config present.  Exiting.", error: true);
                    return true;
                }
            }
            else
            {
                if (this.CheckForCircularConfigs()) return false;
                await ChildLoader.InitializeIntoParents();
            }

            return await handler.Handle();
        }

        public void Init()
        {
            this.Logger = new TypicalLogger(Constants.AppName)
            {
                ConsoleSilent = this.Silent,
                ShouldLogToFile = Settings.Instance.LogToFile,
                WipeLogsOlderThanDays = Settings.Instance.WipeLogsOlderThanDays
            };
            this.Logger.ActivateAndFlushLogging();
            this.ChildLoader = new ChildrenLoader(this);
            this.ConfigLoader = new ConfigLoader(this.TargetPath, this.RepoLoader, this.Logger);
            this.Config = ConfigLoader.GetConfig(this.TargetPath);
        }

        public async Task<bool> CancelIfParentsHaveChanges()
        {
            var uncomittedChangeRepos = await this.GetReposWithUncommittedChanges();
            if (uncomittedChangeRepos.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Cancelling because repos had uncommitted changes:");
                foreach (var repo in uncomittedChangeRepos)
                {
                    sb.AppendLine($"   -{repo.Item1.Nickname}: {repo.Item2}");
                }
                var ret = this.Logger.LogErrorRetry(
                    sb.ToString(),
                    "Confirm Safety Bypass",
                    Settings.Instance.ShowMessageBoxes);
                if (ret == null)
                {
                    return await CancelIfParentsHaveChanges();
                }
                return !ret.Value;
            }
            return false;
        }

        public async Task<List<Tuple<RepoListing, string>>> GetReposWithUncommittedChanges()
        {
            List<Tuple<RepoListing, string>> ret = new List<Tuple<RepoListing, string>>();
            foreach (var repoListing in this.Config.ParentRepos)
            {
                var dirt = await HarmonizeFunctionality.IsDirty(
                    repoListing.Path,
                    this.ConfigLoader,
                    this.RepoLoader,
                    this.Logger);
                if (dirt.Succeeded)
                {
                    this.Logger.WriteLine($"{repoListing.Nickname} was dirty: {dirt.Reason}");
                    ret.Add(new Tuple<RepoListing, string>(repoListing, dirt.Reason));
                }
                else
                {
                    this.Logger.WriteLine($"{repoListing.Nickname} was not dirty.");
                }
            }
            return ret;
        }
        
        public Task<IErrorResponse> IsDirty(
            ConfigExclusion configExclusion = ConfigExclusion.Full,
            bool regenerateConfig = true)
        {
            return HarmonizeFunctionality.IsDirty(
                this.TargetPath, 
                this.ConfigLoader,
                this.RepoLoader, 
                this.Logger,
                configExclusion,
                regenerateConfig);
        }

        public async Task SyncConfigToParentShas()
        {
            this.Logger.WriteLine("Syncing config to parent repo shas.");
            await HarmonizeFunctionality.SyncAndWriteConfig(this.Config, this.TargetPath, this.RepoLoader, this.Logger);
        }

        public void UpdatePathingConfig()
        {
            this.ConfigLoader.Config.Pathing.WriteToPath(this.TargetPath);
        }

        public bool SyncParentRepos()
        {
            return SyncParentRepos(this.Config);
        }

        public bool SyncParentRepos(HarmonizeConfig config)
        {
            bool passed = true;
            foreach (var listing in config.ParentRepos)
            {
                try
                {
                    if (!HarmonizeFunctionality.SyncParentRepo(
                        listing,
                        this.Logger,
                        this.RepoLoader))
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    passed = this.Logger.LogError(
                        $"Error syncing parent repo {listing.Nickname}: {ex}",
                        "Error Syncing Parents",
                        Settings.Instance.ShowMessageBoxes)
                        && passed;
                }
            }
            return passed;
        }
        
        public bool SyncParentReposToSha(string targetCommitSha)
        {
            HarmonizeConfig targetConfig;
            var repo = this.Repo;
            var targetCommit = repo.Lookup<Commit>(targetCommitSha);
            if (targetCommit == null)
            {
                foreach (var origin in repo.Network.Remotes)
                {
                    repo.Fetch(origin.Name);
                }
                targetCommit = repo.Lookup<Commit>(targetCommitSha);
                if (targetCommit == null)
                {
                    throw new ArgumentException("Target commit does not exist. " + targetCommitSha);
                }
            }

            targetConfig = HarmonizeConfig.Factory(
                this.ConfigLoader,
                this.RepoLoader.GetRepo(this.TargetPath),
                targetCommit);
            if (targetConfig == null) return true;
            return SyncParentRepos(targetConfig);
        }

        public bool CheckForCircularConfigs()
        {
            if (!Settings.Instance.CheckForCircularConfigs) return false;
            this.Logger.WriteLine("Checking for circular configs.");
            var ret = CheckCircular(new HashSet<string>(), this.TargetPath);
            if (ret != null)
            {
                return !this.Logger.LogError(
                    $"Found circular configurations:" + Environment.NewLine + ret,
                    "Circular Configs",
                    Settings.Instance.ShowMessageBoxes);
            }
            this.Logger.WriteLine("No circular configs detected.");
            return false;
        }

        private string CheckCircular(HashSet<string> paths, string targetPath)
        {
            if (!paths.Add(targetPath))
            {
                return targetPath;
            }
            var config = this.ConfigLoader.GetConfig(targetPath);
            if (config == null) return null;
            foreach (var listing in config.ParentRepos)
            {
                var ret = CheckCircular(new HashSet<string>(paths), listing.Path);
                if (ret != null) return targetPath + Environment.NewLine + ret;
            }
            return null;
        }

        public virtual void Dispose()
        {
            if (_disposed)
            {
                throw new AccessViolationException("Accessed already disposed object.");
            }
            if (this._RepoLoader.IsValueCreated)
            {
                this._RepoLoader.Value.Dispose();
            }
            _disposed = true;
        }
    }
}
