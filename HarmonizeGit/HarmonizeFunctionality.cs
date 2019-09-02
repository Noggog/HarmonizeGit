using FishingWithGit.Common;
using LibGit2Sharp;
using Noggog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HarmonizeGit
{
    public class HarmonizeFunctionality
    {
        public static bool SyncParentRepo(
            RepoListing listing,
            ILogger logger,
            RepoLoader repoLoader)
        {
            logger.WriteLine($"Processing {listing.Nickname} at path {listing.Path}. Trying to check out an existing branch at {listing.Sha}.");
            if (listing.Sha == null)
            {
                throw new ArgumentException("Listing did not have a sha.");
            }

            var repo = repoLoader.GetRepo(listing.Path);
            if (repo.Head.Tip.Sha.Equals(listing.Sha))
            {
                logger.WriteLine("Repository already at desired commit.");
                return false;
            }

            repo.Discard(Constants.HarmonizeConfigPath);
            if (repo.RetrieveStatus().IsDirty)
            {
                var ret = logger.LogErrorRetry(
                    $"{listing.Nickname} Parent is dirty and cannot be synced.",
                    $"Confim Skip {listing.Nickname} Sync",
                    Settings.Instance.ShowMessageBoxes);
                if (ret == null) return SyncParentRepo(listing, logger, repoLoader);
                return ret.Value;
            }

            var localBranches = new HashSet<string>(
                repo.Branches
                .Where((b) => !b.IsRemote)
                .Select((b) => b.Name()));

            var potentialBranches = repo.Branches
                .Where((b) => b.Tip.Sha.Equals(listing.Sha))
                .Where((b) => !b.Name().Equals("HEAD"))
                .Where((b) => !b.IsRemote || !localBranches.Contains(b.Name()))
                .OrderBy((b) => b.FriendlyName.Contains(Constants.BranchName) ? 0 : 1);

            var existingBranch = potentialBranches
                .Where((b) => !b.IsRemote)
                .FirstOrDefault();
            if (existingBranch == null)
            {
                existingBranch = potentialBranches.FirstOrDefault();
            }

            if (existingBranch != null)
            {
                logger.WriteLine($"Checking out existing branch {listing.Nickname}:{existingBranch.FriendlyName}.");
                var branch = repo.Branches[existingBranch.Name()];
                if (branch == null)
                {
                    branch = repo.CreateBranch(existingBranch.Name(), existingBranch.Tip);
                }
                LibGit2Sharp.Commands.Checkout(repo, branch);
                return true;
            }

            logger.WriteLine("No branch found.  Allocating a Harmonize branch.");

            var targetCommit = repo.Lookup<Commit>(listing.Sha);
            if (targetCommit == null)
            {
                logger.WriteLine("Fetching to locate target commit.");
                foreach (var remote in repo.Network.Remotes)
                {
                    repo.Fetch(remote.Name);
                }

                targetCommit = repo.Lookup<Commit>(listing.Sha);
                if (targetCommit == null)
                {
                    logger.WriteLine("No branch found.  Could not allocate new branch as target commit could not be located.", error: true);
                    return false;
                }
            }

            for (int i = 0; i < 100; i++)
            {
                var branchName = Constants.BranchName + (i == 0 ? "" : i.ToString());
                var harmonizeBranch = repo.Branches[branchName];
                if (harmonizeBranch == null)
                { // Create new branch
                    logger.WriteLine($"Creating {listing.Nickname}:{branchName}.");
                    var branch = repo.CreateBranch(branchName, listing.Sha);
                    Commands.Checkout(repo, branch);
                    return true;
                }
                else if (repo.IsLoneTip(harmonizeBranch))
                {
                    logger.WriteLine(harmonizeBranch.FriendlyName + " was unsafe to move.");
                    continue;
                }
                else
                {
                    logger.WriteLine($"Moving {listing.Nickname}:{harmonizeBranch.FriendlyName} to target commit.");
                    Commands.Checkout(repo, harmonizeBranch);
                    repo.Reset(ResetMode.Hard, listing.Sha);
                    return true;
                }
            }
            throw new NotImplementedException("Delete some branches.  You have over 100.");
        }

        public static bool TryLoadConfig(
            string path,
            RepoLoader repoLoader,
            out HarmonizeConfig config)
        {
            FileInfo file = new FileInfo(path + "/" + Constants.HarmonizeConfigPath);
            if (!file.Exists)
            {
                config = default;
                return false;
            }
            var pathing = PathingConfig.Factory(path);
            using (var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            {
                config = HarmonizeConfig.Factory(
                    repoLoader,
                    path,
                    stream,
                    pathing);
                return true;
            }
        }

        public static async Task SyncAndWriteConfig(HarmonizeConfig config, string path, RepoLoader repoLoader, ILogger logger)
        {
            List<RepoListing> changed = new List<RepoListing>();
            changed.AddRange((await Task.WhenAll(config.ParentRepos.Select(
                (listing) =>
                {
                    return Task.Run(() =>
                    {
                        logger?.WriteLine($"Checking for sha changes {listing.Nickname} at path {listing.Path}.");
                        var repo = repoLoader.GetRepo(listing.Path);
                        logger?.WriteLine($"Config sha {listing.Sha} compared to current sha {repo.Head.Tip.Sha}.");
                        if (object.Equals(listing.Sha, repo.Head.Tip.Sha)) return null;
                        listing.SetToCommit(repo.Head.Tip);
                        if (string.IsNullOrWhiteSpace(listing.OriginHint))
                        {
                            var origin = repo.Network.Remotes.FirstOrDefault(r => "origin".Equals(r.Name));
                            if (origin == null)
                            {
                                origin = repo.Network.Remotes.FirstOrDefault();
                            }
                            listing.OriginHint = origin?.PushUrl ?? null;
                        }
                        logger?.WriteLine($"Changed to sha {repo.Head.Tip.Sha}.");
                        return listing;
                    });
                })))
                .Where((listing) => listing != null));

            if (WriteConfig(config, path, logger))
            {
                if (changed.Count > 0)
                {
                    logger?.WriteLine("Parent repos have changed: ");
                    foreach (var change in changed)
                    {
                        logger?.WriteLine("  " + change.Nickname);
                    }
                }
            }
        }

        public static bool WriteConfig(HarmonizeConfig config, string path, ILogger logger)
        {
            if (object.Equals(config, config?.OriginalConfig)) return false;

            path = path + "/" + Constants.HarmonizeConfigPath;
            logger?.WriteLine($"Updating config at {path}");

            using (LockManager.GetLock(LockType.Harmonize, path))
            {
                config.WriteToPath(path);
            }
            return true;
        }
        
        public static async Task<IErrorResponse> IsDirty(
            string path,
            ConfigLoader configLoader,
            RepoLoader repoLoader,
            ILogger logger,
            ConfigExclusion configExclusion = ConfigExclusion.Full,
            bool regenerateConfig = false)
        {
            var repo = repoLoader.GetRepo(path);
            var repoStatus = repo.RetrieveStatus(new StatusOptions()
            {
                IncludeIgnored = false,
                IncludeUnaltered = false,
                RecurseIgnoredDirs = false,
                ExcludeSubmodules = true
            });
            if (!repoStatus.IsDirty)
            {
                return ErrorResponse.Failure;
            }

            // If just harmonize config that is dirty, then not dirty
            if (!repoStatus.Added.Any()
                && !repoStatus.Removed.Any()
                && !repoStatus.Missing.Any()
                && !repoStatus.Untracked.Any()
                && !repoStatus.RenamedInWorkDir.Any()
                && !repoStatus.RenamedInIndex.Any()
                && !repoStatus.Modified.CountGreaterThan(1))
            {
                var entry = repoStatus.Modified.First();
                if (HarmonizeGit.Constants.HarmonizeConfigPath.Equals(entry.FilePath)) return ErrorResponse.Failure;
            }

            if (regenerateConfig)
            {
                // Regenerate harmonize config, see if that cleans it
                var status = repo.RetrieveStatus(Constants.HarmonizeConfigPath);
                if (status != FileStatus.Unaltered
                    && status != FileStatus.Nonexistent)
                {
                    var parentConfig = configLoader.GetConfig(path);
                    if (parentConfig != null)
                    {
                        await HarmonizeFunctionality.SyncAndWriteConfig(parentConfig, path, repoLoader, logger);
                        repoStatus = repo.RetrieveStatus();
                        if (!repoStatus.IsDirty)
                        {
                            return ErrorResponse.Failure;
                        }
                    }
                }
            }

            foreach (var statusEntry in repoStatus)
            {
                if (statusEntry.FilePath.Equals(Constants.HarmonizeConfigPath) && configExclusion == ConfigExclusion.Full) continue;
                if (statusEntry.State == FileStatus.Unaltered) continue;
                if (statusEntry.State.HasFlag(FileStatus.Ignored)) continue;

                // Wasn't just harmonize config, it's dirty
                return ErrorResponse.Succeed($"{statusEntry.State} - {statusEntry.FilePath}");
            }
            return ErrorResponse.Failure;
        }
    }
}
