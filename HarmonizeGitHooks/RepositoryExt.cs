using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGitHooks
{
    public static class RepositoryExt
    {
        public static bool IsLoneTip(this Repository repo, Branch targetBranch, string sha)
        {
            foreach (var branch in repo.ListBranchesContaininingCommit(sha))
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
    }
}
