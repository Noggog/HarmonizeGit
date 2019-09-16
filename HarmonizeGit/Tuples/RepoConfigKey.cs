using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Noggog;

namespace HarmonizeGit
{
    public class RepoConfigKey : IEquatable<RepoConfigKey>
    {
        public string WorkingDir;
        public string CommitSha;

        public bool Equals(RepoConfigKey other)
        {
            if (other == null) return false;
            if (!object.Equals(this.WorkingDir, other.WorkingDir)) return false;
            if (!object.Equals(this.CommitSha, other.CommitSha)) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is RepoConfigKey rhs)) return false;
            return Equals(rhs);
        }

        public override int GetHashCode()
        {
            return HashHelper.GetHashCode(
                WorkingDir, 
                CommitSha);
        }
    }
}
