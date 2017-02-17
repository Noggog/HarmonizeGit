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

        public override bool Handle(List<string> args)
        {
            if (this.harmonize.CancelIfParentsHaveChanges())
            {
                return false;
            }

            this.harmonize.SyncConfigToParentShas();
            using (var repo = new Repository("."))
            {
                Commands.Stage(repo, HarmonizeGitBase.HarmonizeConfigPath);
            }
            return true;
        }
    }
}
