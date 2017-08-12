using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit.RepoTranslator
{
    public class RepoTranslator
    {
        public IEnumerable<string> GetHarmonizeRepos()
        {
            DirectoryInfo root = new DirectoryInfo(
                Properties.Settings.Default.SourceReposRoot);
            if (!root.Exists) yield break;
            foreach (var harmonizeConfig in root.EnumerateFiles(Constants.HarmonizeConfigPath, SearchOption.AllDirectories))
            {
            }
        }
    }
}
