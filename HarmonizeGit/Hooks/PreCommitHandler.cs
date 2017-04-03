using FishingWithGit;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace HarmonizeGit
{
    public class PreCommitHandler : TypicalHandlerBase
    {
        public PreCommitHandler(HarmonizeGitBase harmonize)
            : base(harmonize)
        {
        }

        public override async Task<bool> Handle(string[] args)
        {
            CommitArgs commitArgs = new CommitArgs(args);

            if (this.harmonize.CancelIfParentsHaveChanges())
            {
                return false;
            }

            if (commitArgs.Amending)
            {
                this.harmonize.WriteLine("Running amending commit tasks.");
                using (var repo = new Repository(this.harmonize.TargetPath))
                {
                    var resetRet = await PreResetHandler.DoResetTasks(
                        this.harmonize,
                        repo,
                        new Commit[] { repo.Head.Tip });
                    if (!resetRet) return false;
                }
            }

            DoCommitTasks();
            return true;
        }

        private void DoCommitTasks()
        {
            this.harmonize.SyncConfigToParentShas();
            using (var repo = new Repository(this.harmonize.TargetPath))
            {
                Commands.Stage(repo, HarmonizeGitBase.HarmonizeConfigPath);
            }
        }
    }
}
