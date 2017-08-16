using LibGit2Sharp;
using Noggog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit.RepoTranslator
{
    public class RepoTranslator : IDisposable
    {
        public RepoLoader Repos = new RepoLoader();
        public TranslatorSpec Spec;

        private Dictionary<DirectoryPath, HarmonizeConfig> configs = new Dictionary<DirectoryPath, HarmonizeConfig>();

        public RepoTranslator(string specPath)
        {
            this.Spec = TranslatorSpec.Create_XML(specPath);
        }

        public void Dispose()
        {
            this.Repos.Dispose();
        }

        public async Task Translate()
        {
            if (this.Spec.ExportFolder.Exists)
            {
                throw new ArgumentException("Directory already exists.  Delete it first: " + this.Spec.ExportFolder);
            }

            switch (this.Spec.Type)
            {
                case TranslationType.DirectoryRepo:
                    await TranslateToDirectoryRepo();
                    break;
                case TranslationType.Monolithic:
                default:
                    throw new NotImplementedException();
            }
        }

        private async Task TranslateToDirectoryRepo()
        {
            Repository.Init(this.Spec.ExportFolder.Path);
            var repo = this.Repos.GetRepo(this.Spec.ExportFolder.Path);
            
            foreach (var targetRepo in this.Spec.TargetRepos)
            {
                var targetRepo
            }
        }
    }
}
