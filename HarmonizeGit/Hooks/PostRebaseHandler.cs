﻿using FishingWithGit;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit
{
    public class PostRebaseHandler : TypicalHandlerBase
    {
        RebaseInProgressArgs args;
        public override IGitHookArgs Args => args;

        public PostRebaseHandler(HarmonizeGitBase harmonize, RebaseInProgressArgs args)
            : base(harmonize)
        {
            this.NeedsConfig = false;
            this.args = args;
        }

        public override async Task<bool> Handle()
        {
            using (var repo = new Repository(this.harmonize.TargetPath))
            {
                var originalCommit = repo.Lookup<Commit>(args.OriginalTipSha);
                if (originalCommit == null)
                {
                    harmonize.WriteLine($"Original commit {args.OriginalTipSha} could not be found.");
                    return false;
                }

                var landingCommit = repo.Lookup<Commit>(args.LandingSha);
                if (landingCommit == null)
                {
                    harmonize.WriteLine($"Target commit {args.LandingSha} could not be found.");
                    return false;
                }

                // Remove old commits from usage
                if (!PreRebaseHandler.GetStrandedCommits(
                    this.harmonize,
                    repo,
                    originalCommit,
                    landingCommit,
                    out IEnumerable<Commit> strandedCommits))
                {
                    return false;
                }

                await PreResetHandler.RemoveFromParentDatabase(
                    this.harmonize,
                    repo,
                    strandedCommits);

                // Insert new commits to usage
                if (!PreRebaseHandler.GetStrandedCommits(
                    this.harmonize,
                    repo,
                    repo.Head.Tip,
                    landingCommit,
                    out IEnumerable<Commit> newCommits))
                {
                    return false;
                }

                await this.harmonize.ChildLoader.InsertChildEntries(
                    this.harmonize.ChildLoader.GetUsages(
                        repo,
                        newCommits));
            }
            return true;
        }
    }
}
