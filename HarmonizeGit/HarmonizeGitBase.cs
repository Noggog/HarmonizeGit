﻿using FishingWithGit;
using FishingWithGit.Common;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
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
    public class HarmonizeGitBase
    {
        public ConfigLoader ConfigLoader { get; private set; }
        public ChildrenLoader ChildLoader { get; private set; }
        private Lazy<RepoLoader> _RepoLoader = new Lazy<RepoLoader>();
        public RepoLoader RepoLoader => _RepoLoader.Value;
        public const string BranchName = "HarmonizeGit";
        public const string AppName = "HarmonizeGit";
        public const string HarmonizeConfigPath = ".harmonize";
        public const string HarmonizeChildrenPath = ".git/.harmonize-children";
        public const string HarmonizeChildrenDBPath = ".git/.harmonize-children.db";
        public const string HarmonizePathingPath = ".git/.harmonize-pathing";
        public const string HarmonizeEXEName = "HarmonizeGit.exe";
        public const string HarmonizeEXEPath = "HarmonizeGit/HarmonizeGit.exe";
        public const string GitIgnorePath = ".gitignore";
        public readonly string TargetPath;
        public string ConfigPath => Path.Combine(TargetPath, HarmonizeConfigPath);
        public HarmonizeConfig Config;
        public bool Silent;
        public bool FileLock;
        public Logger Logger;
        public Repository Repo => this.RepoLoader.GetRepo(this.TargetPath);

        public HarmonizeGitBase(string targetPath)
        {
            this.TargetPath = targetPath;
        }

        public async Task<bool> Handle(string[] args)
        {
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
                    this.Logger.LogResults();
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
            this.Logger = new Logger(HarmonizeGitBase.AppName)
            {
                ConsoleSilent = this.Silent,
                ShouldLogToFile = Settings.Instance.LogToFile,
                WipeLogsOlderThanDays = Settings.Instance.WipeLogsOlderThanDays 
            };
            this.Logger.ActivateAndFlushLogging();
            this.ChildLoader = new ChildrenLoader(this);
            this.ConfigLoader = new ConfigLoader();
            this.ConfigLoader.Init(this);
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
                return this.Logger.LogErrorYesNo(
                    sb.ToString(),
                    "Parents Have Uncommitted Changes",
                    Settings.Instance.ShowMessageBoxes);
            }
            return false;
        }

        public async Task<List<Tuple<RepoListing, string>>> GetReposWithUncommittedChanges()
        {
            List<Tuple<RepoListing, string>> ret = new List<Tuple<RepoListing, string>>();
            foreach (var repoListing in this.Config.ParentRepos)
            {
                var dirt = await IsDirty(repoListing.Path);
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

        #region IsDirty
        public Task<ErrorResponse> IsDirty(
            ConfigExclusion configExclusion = ConfigExclusion.Full,
            bool regenerateConfig = true)
        {
            return IsDirty(this.TargetPath, configExclusion, regenerateConfig);
        }

        public async Task<ErrorResponse> IsDirty(
            string path,
            ConfigExclusion configExclusion = ConfigExclusion.Full,
            bool regenerateConfig = true)
        {
            var repo = this.RepoLoader.GetRepo(path);
            var repoStatus = repo.RetrieveStatus(new StatusOptions()
            {
                IncludeIgnored = false,
                IncludeUnaltered = false,
                RecurseIgnoredDirs = false,
                ExcludeSubmodules = true
            });
            if (!repoStatus.IsDirty)
            {
                return new ErrorResponse();
            }

            if (regenerateConfig)
            {
                // Regenerate harmonize config, see if that cleans it
                var status = repo.RetrieveStatus(HarmonizeConfigPath);
                if (status != FileStatus.Unaltered
                    && status != FileStatus.Nonexistent)
                {
                    var parentConfig = ConfigLoader.GetConfig(path);
                    if (parentConfig != null)
                    {
                        await this.ConfigLoader.SyncAndWriteConfig(parentConfig, path);
                        repoStatus = repo.RetrieveStatus();
                        if (!repoStatus.IsDirty)
                        {
                            return new ErrorResponse();
                        }
                    }
                }
            }

            foreach (var statusEntry in repoStatus)
            {
                if (statusEntry.FilePath.Equals(HarmonizeConfigPath) && configExclusion == ConfigExclusion.Full) continue;
                if (statusEntry.State == FileStatus.Unaltered) continue;
                if (statusEntry.State.HasFlag(FileStatus.Ignored)) continue;

                // Wasn't just harmonize config, it's dirty
                return new ErrorResponse()
                {
                    Succeeded = true,
                    Reason = $"{statusEntry.State} - {statusEntry.FilePath}"
                };
            }
            return new ErrorResponse();
        }
        #endregion

        public async Task SyncConfigToParentShas()
        {
            this.Logger.WriteLine("Syncing config to parent repo shas.");
            await this.ConfigLoader.SyncAndWriteConfig(this.Config, this.TargetPath);
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
                    if (!SyncParentRepo(listing)) return false;
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

        private bool SyncParentRepo(RepoListing listing)
        {
            this.Logger.WriteLine($"Processing {listing.Nickname} at path {listing.Path}. Trying to check out an existing branch at {listing.Sha}.");
            if (listing.Sha == null)
            {
                throw new ArgumentException("Listing did not have a sha.");
            }

            var repo = this.RepoLoader.GetRepo(listing.Path);
            if (repo.Head.Tip.Sha.Equals(listing.Sha))
            {
                this.Logger.WriteLine("Repository already at desired commit.");
                return true;
            }

            repo.Discard(HarmonizeGitBase.HarmonizeConfigPath);
            if (repo.RetrieveStatus().IsDirty)
            {
                this.Logger.WriteLine($"Checking out existing branch error {listing.Nickname}: was still dirty after cleaning config.", error: true);
                return false;
            }

            var localBranches = new HashSet<string>(
                repo.Branches
                .Where((b) => !b.IsRemote)
                .Select((b) => b.Name()));

            var potentialBranches = repo.Branches
                .Where((b) => b.Tip.Sha.Equals(listing.Sha))
                .Where((b) => !b.Name().Equals("HEAD"))
                .Where((b) => !b.IsRemote || !localBranches.Contains(b.Name()))
                .OrderBy((b) => b.FriendlyName.Contains(BranchName) ? 0 : 1);

            var existingBranch = potentialBranches
                .Where((b) => !b.IsRemote)
                .FirstOrDefault();
            if (existingBranch == null)
            {
                existingBranch = potentialBranches.FirstOrDefault();
            }

            if (existingBranch != null)
            {
                this.Logger.WriteLine($"Checking out existing branch {listing.Nickname}:{existingBranch.FriendlyName}.");
                var branch = repo.Branches[existingBranch.Name()];
                if (branch == null)
                {
                    branch = repo.CreateBranch(existingBranch.Name(), existingBranch.Tip);
                }
                LibGit2Sharp.Commands.Checkout(repo, branch);
                return true;
            }
            this.Logger.WriteLine("No branch found.  Allocating a Harmonize branch.");
            for (int i = 0; i < 100; i++)
            {
                var branchName = BranchName + (i == 0 ? "" : i.ToString());
                var harmonizeBranch = repo.Branches[branchName];
                if (harmonizeBranch == null)
                { // Create new branch
                    this.Logger.WriteLine($"Creating {listing.Nickname}:{branchName}.");
                    var branch = repo.CreateBranch(branchName, listing.Sha);
                    Commands.Checkout(repo, branch);
                    return true;
                }
                else if (repo.IsLoneTip(harmonizeBranch))
                {
                    this.Logger.WriteLine(harmonizeBranch.FriendlyName + " was unsafe to move.");
                    continue;
                }
                else
                {
                    this.Logger.WriteLine($"Moving {listing.Nickname}:{harmonizeBranch.FriendlyName} to target commit.");
                    Commands.Checkout(repo, harmonizeBranch);
                    repo.Reset(ResetMode.Hard, listing.Sha);
                    return true;
                }
            }
            throw new NotImplementedException("Delete some branches.  You have over 100.");
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
                this,
                this.TargetPath,
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
    }
}
