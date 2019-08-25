using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit
{
    public class RepoLoader : IDisposable
    {
        readonly string _targetPath;
        Dictionary<string, Repository> repos = new Dictionary<string, Repository>();

        public RepoLoader(string targetPath)
        {
            this._targetPath = targetPath;
        }

        public Repository GetRepo(string path)
        {
            path = FishingWithGit.Common.Utility.StandardizePath(path, _targetPath);
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
