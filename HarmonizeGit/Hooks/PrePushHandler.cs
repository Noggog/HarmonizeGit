using FishingWithGit;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit
{
    public class PrePushHandler : TypicalHandlerBase
    {
        PushArgs args;
        public override IGitHookArgs Args => args;

        public PrePushHandler(HarmonizeGitBase harmonize, PushArgs args)
            : base(harmonize)
        {
            this.args = args;
        }

        public override async Task<bool> Handle()
        {
            if (Settings.Instance.ParentUnpushedPreference == ParentPushPreference.Nothing) return true;

            var branchesBeingPushed = new HashSet<string>(args.RefSpecs.Select((r) => r.Item1));
            var pushingConfigs = new List<Tuple<string, HarmonizeConfig>>();
            using (var repo = new Repository(this.harmonize.TargetPath))
            {
                var branches = repo.Branches
                    .Where((b) => branchesBeingPushed.Contains(b.Name()))
                    .Where((b) => !b.IsRemote)
                    .ToArray();
                pushingConfigs.AddRange(branches.Select((b) =>
                {
                    return new Tuple<string, HarmonizeConfig>(
                        b.Name(),
                        this.harmonize.ConfigLoader.GetConfigFromRepo(repo, b.Tip));
                })
                .Where((i) => i.Item2 != null));
            }

            HashSet<string> fetchedRemotes = new HashSet<string>();
            foreach (var config in pushingConfigs)
            {
                foreach (var repoListing in config.Item2.ParentRepos)
                {
                    using (var parentRepo = new Repository(repoListing.Path))
                    {
                        var remoteNames = new HashSet<string>(parentRepo.Network.Remotes.Select((r) => r.Name));
                        if (remoteNames.Count == 0) continue;

                        var branchesTouchingCommit = parentRepo.ListBranchesContainingCommit(repoListing.Sha)
                            .Where((b) => b.IsRemote)
                            .ToArray();

                        // Remove remotes who have branches touching the desired commit
                        foreach (var branch in branchesTouchingCommit)
                        {
                            remoteNames.Remove(branch.RemoteName);
                        }
                        if (remoteNames.Count == 0) continue;

                        // Fetch failed remotes
                        foreach (var remote in parentRepo.Network.Remotes
                            .Where((r) => remoteNames.Contains(r.Name)))
                        {
                            if (fetchedRemotes.Add(remote.Name))
                            {
                                parentRepo.Network.Fetch(remote);
                            }
                        }

                        //  Try Again
                        foreach (var branch in branchesTouchingCommit)
                        {
                            remoteNames.Remove(branch.RemoteName);
                        }
                        if (remoteNames.Count == 0) continue;


                        // Have some remotes that don't know about our config's reference
                        this.harmonize.WriteLine("Blocking because parent repositories need to push their branches first:");
                        foreach (var remoteName in remoteNames)
                        {
                            this.harmonize.WriteLine($"   {repoListing.Nickname} -> {config.Item1}");
                        }
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
