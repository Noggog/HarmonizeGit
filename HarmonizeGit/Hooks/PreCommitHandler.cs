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
        CommitArgs args;
        public override IGitHookArgs Args => args;

        public PreCommitHandler(HarmonizeGitBase harmonize, CommitArgs args)
            : base(harmonize)
        {
            this.args = args;
        }

        public override async Task<bool> Handle()
        {
            if (await this.harmonize.CancelIfParentsHaveChanges())
            {
                return false;
            }

            if (args.Amending)
            {
                this.harmonize.Logger.WriteLine("Running amending commit tasks.");
                var repo = this.harmonize.Repo;
                var resetRet = await PreResetHandler.DoResetTasks(
                    this.harmonize,
                    repo,
                    new Commit[] { repo.Head.Tip });
                if (!resetRet) return false;
            }

            await DoCommitTasks();
            return true;
        }

        private async Task DoCommitTasks()
        {
            await this.harmonize.SyncConfigToParentShas();
            Commands.Stage(this.harmonize.Repo, Constants.HarmonizeConfigPath);
        }
    }
}
