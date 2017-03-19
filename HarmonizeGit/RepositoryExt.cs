using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit
{
    public static class RepositoryExt
    {
        public static IEnumerable<Commit> GetPotentiallyStrandedCommits(
            this Repository repo,
            Commit tip,
            Commit ancestor)
        {
            Queue<Commit> toDo = new Queue<Commit>();
            toDo.Enqueue(tip);
            HashSet<string> processedShas = new HashSet<string>();
            while (toDo.Count > 0)
            {
                var item = toDo.Dequeue();
                // If we reached our target commit, we're done
                if (ancestor.Sha.Equals(item.Sha)) continue;
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
            foreach (var branch in repo.Branches)
            {
                foreach (var commit in repo.Commits.QueryBy(
                    new CommitFilter()
                    {
                        IncludeReachableFrom = branch.Tip.Sha,
                    }))
                {
                    if (commit.Author.When < targetCommit.Author.When)
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

        public static string Name(this Branch b)
        {
            var index = b.FriendlyName.IndexOf("/");
            if (index == -1) return b.FriendlyName;
            return b.FriendlyName.Substring(index + 1);
        }
    }
}
