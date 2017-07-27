using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit
{
    public class RepoLoader : IDisposable
    {
        Dictionary<string, Repository> repos = new Dictionary<string, Repository>();

        public Repository GetRepo(string path)
        {
            path = FishingWithGit.Common.Utility.StandardizePath(path);
            if (repos.TryGetValue(path, out var repo)) return repo;
            repo = new Repository(path);
            repos[path] = repo;
            return repo;
        }

        public void Dispose()
        {
            foreach (var repo in repos.Values)
            {
                repo.Dispose();
            }
        }
    }
}
