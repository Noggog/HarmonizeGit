using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGitHooks
{
    public class PathingListing
    {
        public string Nickname;
        public string Path;

        public override bool Equals(object obj)
        {
            PathingListing rhs = obj as PathingListing;
            if (rhs == null) return false;
            if (!object.Equals(Nickname, rhs.Nickname)) return false;
            if (!object.Equals(Path, rhs.Path)) return false;
            return true;
        }
    }
}
