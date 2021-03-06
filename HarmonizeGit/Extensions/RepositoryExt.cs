﻿using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit
{
    public static class RepositoryExt
    {
        public static void Discard(this IRepository repo, params string[] paths)
        {
            repo.CheckoutPaths(
                committishOrBranchSpec: repo.Head.Tip.Sha,
                paths: paths,
                checkoutOptions: new CheckoutOptions()
                {
                    CheckoutModifiers = CheckoutModifiers.Force
                });
        }

        public static IEnumerable<Commit> GetPotentiallyStrandedCommits(
            this Repository repo,
            Commit tip,
            Commit ancestor = null)
        {
            Queue<Commit> toDo = new Queue<Commit>();
            toDo.Enqueue(tip);
            HashSet<string> processedShas = new HashSet<string>();
            while (toDo.Count > 0)
            {
                var item = toDo.Dequeue();
                // If we reached our target commit, we're done
                if (ancestor?.Sha.Equals(item.Sha) ?? false) continue;
                // If we've already processed, short circuit
                if (!processedShas.Add(item.Sha)) continue;
                // If another branch contains this commit, it's safe
                if (repo.ListBranchesContainingCommit(item.Sha).Any((b) => !object.Equals(b.CanonicalName, repo.Head.CanonicalName))) continue;
                // Stranded commit
                yield return item;
                // Add and see if parents are also stranded
                foreach (var parent in item.Parents)
                {
                    toDo.Enqueue(parent);
                }
            }
        }

        public static bool IsLoneTip(this Repository repo, Branch targetBranch)
        {
            foreach (var branch in repo.ListBranchesContainingCommit(targetBranch.Tip.Sha))
            {
                if (branch.Equals(targetBranch)) continue;
                return false;
            }
            return true;
        }

        public static IEnumerable<Branch> ListBranchesContainingCommit(this Repository repo, string commitSha)
        {
            var targetCommit = repo.Lookup<Commit>(commitSha);
            if (targetCommit == null) yield break;
            foreach (var branch in repo.Branches)
            {
                foreach (var commit in repo.Commits.QueryBy(
                    new CommitFilter()
                    {
                        IncludeReachableFrom = branch.Tip.Sha,
                        SortBy = CommitSortStrategies.Time
                    }))
                {
                    if (commit.Committer.When < targetCommit.Committer.When)
                    { // Not going to be found earlier than target commit
                        break;
                    }
                    if (object.Equals(commit.Sha, commitSha))
                    {
                        yield return branch;
                        break;
                    }
                }
            }
        }

        public static async Task InsertStrandedCommitsIntoParent(
            this Repository repo,
            HarmonizeGitBase harmonize,
            Commit curTip,
            Commit ancestorTip)
        {
            var strandedCommits = repo.GetPotentiallyStrandedCommits(
                curTip,
                ancestorTip);

            var usages = strandedCommits.SelectMany(
                (commit) =>
                {
                    return harmonize.ChildLoader.GetUsagesFromConfig(
                        harmonize.ConfigLoader.GetConfigFromRepo(
                            repo,
                            commit),
                        commit.Sha);
                });

            await harmonize.ChildLoader.InsertChildEntries(usages);
        }

        public static string Name(this Branch b)
        {
            var index = b.FriendlyName.IndexOf("/");
            if (index == -1) return b.FriendlyName;
            return b.FriendlyName.Substring(index + 1);
        }
    }
}
