using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit
{
    class PostCommitHandler : TypicalHandlerBase
    {
        public PostCommitHandler(HarmonizeGitBase harmonize)
            : base(harmonize)
        {
        }

        public override async Task<bool> Handle(string[] args)
        {
            await this.harmonize.ChildLoader.InsertCurrentConfig();
            return true;
        }
    }
}
