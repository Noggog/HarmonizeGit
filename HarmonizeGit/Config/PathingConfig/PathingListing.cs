using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit
{
    public class PathingListing : IEquatable<PathingListing>
    {
        public string Nickname;
        public string Path;

        public bool Equals(PathingListing other)
        {
            if (other == null) return false;
            if (!object.Equals(this.Nickname, other.Nickname)) return false;
            if (!object.Equals(this.Path, other.Path)) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PathingListing listing)) return false;
            return Equals(listing);
        }

        public override int GetHashCode()
        {
            return HashHelper.GetHashCode(
                this.Nickname,
                this.Path);
        }

        public PathingListing GetCopy()
        {
            return new PathingListing()
            {
                Nickname = this.Nickname,
                Path = this.Path
            };
        }
    }
}
