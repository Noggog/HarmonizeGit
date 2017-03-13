using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace HarmonizeGitHooks
{
    class PreCommitHandler : TypicalHandlerBase
    {
        public PreCommitHandler(HarmonizeGitBase harmonize)
            : base(harmonize)
        {
        }

        public override async Task<bool> Handle(string[] args)
        {
            if (this.harmonize.CancelIfParentsHaveChanges())
            {
                return false;
            }

            if (args.Contains("--amend"))
            {
                this.harmonize.WriteLine("Running amending commit tasks.");
                using (var repo = new Repository(this.harmonize.TargetPath))
                {
                    await PreResetHandler.DoResetTasks(
                        this.harmonize,
                        repo,
                        new Commit[] { repo.Head.Tip });
                }
            }

            DoCommitTasks();
            return true;
        }

        private void DoCommitTasks()
        {
            this.harmonize.SyncConfigToParentShas();
            this.harmonize.UpdatePathingConfig(trim: true);
            using (var repo = new Repository(this.harmonize.TargetPath))
            {
                Commands.Stage(repo, HarmonizeGitBase.HarmonizeConfigPath);
                Commands.Stage(repo, HarmonizeGitBase.HarmonizePathingPath);
            }
        }
    }
}
