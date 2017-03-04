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
    class CommitHandler : TypicalHandlerBase
    {
        public CommitHandler(HarmonizeGitBase harmonize)
            : base(harmonize)
        {
        }

        public override async Task<bool> Handle(List<string> args)
        {
            if (this.harmonize.CancelIfParentsHaveChanges())
            {
                return false;
            }

            this.harmonize.SyncConfigToParentShas();
            this.harmonize.UpdatePathingConfig(trim: true);
            using (var repo = new Repository(this.harmonize.TargetPath))
            {
                Commands.Stage(repo, HarmonizeGitBase.HarmonizeConfigPath);
                Commands.Stage(repo, HarmonizeGitBase.HarmonizePathingPath);
            }
            return true;
        }
    }
}
