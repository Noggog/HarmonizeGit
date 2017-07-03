using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit
{
    public static class RepoExt
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
    }
}
