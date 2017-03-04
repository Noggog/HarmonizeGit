﻿using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace HarmonizeGitHooks
{
    public class HarmonizeGitBase
    {
        public ConfigLoader ConfigLoader { get; } = new ConfigLoader();
        public ChildrenLoader ChildLoader { get; private set; }
        public const string BranchName = "GitHarmonize";
        public const string HarmonizeConfigPath = ".harmonize";
        public const string HarmonizeChildrenPath = ".git/.harmonize-children";
        public const string HarmonizePathingPath = ".git/.harmonize-pathing";
        public const string GitIgnorePath = ".gitignore";
        public readonly string TargetPath;
        public HarmonizeConfig Config;
        public bool Silent;

        public HarmonizeGitBase(string targetPath)
        {
            this.TargetPath = targetPath;
            ChildLoader = new ChildrenLoader(this);
        }
        
        public async Task<bool> Handle(string[] args)
        {
            if (args.Length < 2) return true;

            TypicalHandlerBase handler;
            switch (args[0])
            {
                case "pre-checkout":
                    handler = new CheckoutHandler(this);
                    break;
                case "post-reset":
                    handler = new ResetHandler(this);
                    break;
                case "pre-commit":
                    handler = new CommitHandler(this);
                    break;
                case "post-status":
                    handler = new StatusHandler(this);
                    break;
                case "post-take":
                    handler = new TakeHandler(this);
                    break;
                default:
                    return true;
            }

            this.Silent = handler.Silent;

            ConfigLoader.Init(this);
            this.Config = ConfigLoader.GetConfig(this.TargetPath);
            if (this.Config == null)
            {
                this.WriteLine("No config present.  Exiting.");
                return true;
            }
            this.CheckForCircularConfigs();
            await ChildLoader.InitializeIntoParents();

            List<string> trimmedArgs = new List<string>();
            for (int i = 2; i < args.Length; i++)
            {
                trimmedArgs.Add(args[i]);
            }
            return await handler.Handle(trimmedArgs);
        }

        public void WriteLine(string line)
        {
            if (!this.Silent)
            {
                System.Console.WriteLine(line);
            }
        }

        public void WriteLine(object line)
        {
            if (!this.Silent)
            {
                System.Console.WriteLine(line);
            }
        }

        public bool CancelIfParentsHaveChanges()
        {
            var uncomittedChangeRepos = this.GetReposWithUncommittedChanges();
            if (uncomittedChangeRepos.Count > 0)
            {
                this.WriteLine("Cancelling because repos had uncommitted changes:");
                foreach (var repo in uncomittedChangeRepos)
                {
                    this.WriteLine("   -" + repo.Nickname);
                }
                return true;
            }
            return false;
        }

        public List<RepoListing> GetReposWithUncommittedChanges()
        {
            List<RepoListing> ret = new List<RepoListing>();
            foreach (var repoListing in this.Config.ParentRepos)
            {
                if (IsDirty(repoListing))
                {
                    this.WriteLine($"{repoListing.Nickname} was dirty.");
                    ret.Add(repoListing);
                }
                else
                {
                    this.WriteLine($"{repoListing.Nickname} was not dirty.");
                }
            }
            return ret;
        }

        private bool IsDirty(RepoListing repoListing)
        {
            using (var repo = new Repository(repoListing.Path))
            {
                if (!repo.RetrieveStatus().IsDirty) return false;
                if (repo.RetrieveStatus(HarmonizeConfigPath) == FileStatus.Unaltered) return true;
            }
            this.WriteLine($"{repoListing.Nickname}'s config was dirty.  Refreshing it and trying again.");
            var parentConfig = ConfigLoader.GetConfig(repoListing.Path);
            this.ConfigLoader.WriteSyncAndConfig(parentConfig, repoListing.Path);
            using (var repo = new Repository(repoListing.Path))
            {
                return repo.RetrieveStatus().IsDirty;
            }
        }

        public void SyncConfigToParentShas()
        {
            this.WriteLine("Syncing config to parent repo shas.");
            this.ConfigLoader.WriteSyncAndConfig(this.Config, this.TargetPath);
        }

        public void UpdatePathingConfig(bool trim)
        {
            this.ConfigLoader.UpdatePathingConfig(this.Config, trim);
        }

        public void SyncParentRepos()
        {
            SyncParentRepos(this.Config);
        }

        public void SyncParentRepos(HarmonizeConfig config)
        {
            foreach (var listing in config.ParentRepos)
            {
                SyncParentRepo(listing);
            }
        }

        private void SyncParentRepo(RepoListing listing)
        {
            this.WriteLine($"Processing {listing.Nickname} at path {listing.Path}. Trying to check out an existing branch at {listing.Sha}.");
            if (listing.Sha == null)
            {
                throw new ArgumentException("Listing did not have a sha.");
            }

            using (var repo = new Repository(listing.Path))
            {
                var potentialBranches = repo.Branches
                    .Where((b) => b.Tip.Sha.Equals(listing.Sha))
                    .OrderBy((b) => b.FriendlyName.Contains("GitHarmonize") ? 0 : 1);

                var existingBranch = potentialBranches
                    .Where((b) => !b.IsRemote)
                    .FirstOrDefault();
                if (existingBranch == null)
                {
                    existingBranch = potentialBranches.FirstOrDefault();
                }

                if (existingBranch != null)
                {
                    this.WriteLine($"Checking out existing branch {listing.Nickname}:{existingBranch.FriendlyName}.");
                    LibGit2Sharp.Commands.Checkout(repo, existingBranch.FriendlyName);
                    return;
                }
                this.WriteLine("No branch found.  Allocating a Harmonize branch.");
                for (int i = 0; i < 100; i++)
                {
                    var branchName = BranchName + (i == 0 ? "" : i.ToString());
                    var harmonizeBranch = repo.Branches[branchName];
                    if (harmonizeBranch == null)
                    { // Create new branch
                        this.WriteLine($"Creating {listing.Nickname}:{branchName}.");
                        var branch = repo.CreateBranch(branchName, listing.Sha);
                        Commands.Checkout(repo, branch);
                        return;
                    }
                    else if (IsLoneTip(repo, harmonizeBranch, harmonizeBranch.Tip.Sha))
                    {
                        this.WriteLine(harmonizeBranch.FriendlyName + " was unsafe to move.");
                        continue;
                    }
                    else
                    {
                        this.WriteLine($"Moving {listing.Nickname}:{harmonizeBranch.FriendlyName} to target commit.");
                        Commands.Checkout(repo, harmonizeBranch);
                        repo.Reset(ResetMode.Hard, listing.Sha);
                        return;
                    }
                }
            }
            throw new NotImplementedException("Delete some branches.  You have over 100.");
        }

        public void SyncParentReposToSha(string targetCommitSha)
        {
            HarmonizeConfig targetConfig;
            using (var repo = new Repository(this.TargetPath))
            {
                var targetCommit = repo.Lookup<Commit>(targetCommitSha);
                if (targetCommit == null)
                {
                    throw new ArgumentException("Target commit does not exist. " + targetCommitSha);
                }

                targetConfig = HarmonizeConfig.Factory(
                    this,
                    this.TargetPath,
                    targetCommit,
                    this.ConfigLoader.GetPathing(this.TargetPath));
                if (targetConfig == null) return;
            }
            SyncParentRepos(targetConfig);
        }

        public bool IsDirty(bool excludeHarmonizeConfig = true)
        {
            using (var repo = new Repository(this.TargetPath))
            {
                return repo.RetrieveStatus().IsDirty;
            }
        }

        public void CheckForCircularConfigs()
        {
            if (!Properties.Settings.Default.CheckForCircularConfigs) return;
            this.WriteLine("Checking for circular configs.");
            var ret = CheckCircular(ImmutableHashSet.Create<string>(), this.TargetPath);
            if (ret != null)
            {
                throw new ArgumentException($"Found circular configurations:" + Environment.NewLine + ret);
            }
        }

        private string CheckCircular(ImmutableHashSet<string> paths, string targetPath)
        {
            if (paths.Contains(targetPath))
            {
                return targetPath;
            }
            paths = paths.Add(targetPath);
            var config = this.ConfigLoader.GetConfig(targetPath);
            if (config == null) return null;
            foreach (var listing in config.ParentRepos)
            {
                var ret = CheckCircular(paths, listing.Path);
                if (ret != null) return targetPath + Environment.NewLine + ret;
            }
            return null;
        }

        private bool IsLoneTip(Repository repo, Branch targetBranch, string sha)
        {
            foreach (var branch in ListBranchesContaininingCommit(repo, sha))
            {
                if (branch.Equals(targetBranch)) continue;
                return false;
            }
            return true;
        }

        private IEnumerable<Branch> ListBranchesContaininingCommit(Repository repo, string commitSha)
        {
            foreach (var branch in repo.Branches)
            {
                var commits = repo.Commits.QueryBy(
                    new CommitFilter()
                    {
                        IncludeReachableFrom = branch.Tip.Sha
                    })
                    .Where(c => c.Sha == commitSha);

                if (!commits.Any())
                {
                    continue;
                }

                yield return branch;
            }
        }
    }
}
