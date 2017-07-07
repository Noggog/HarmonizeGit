using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit
{
    public class GetResponse<T> : IEquatable<GetResponse<T>>
    {
        public T Item;
        public bool Succeeded;

        public bool Equals(GetResponse<T> other)
        {
            if (other == null) return false;
            if (this.Succeeded != other.Succeeded) return false;
            if (!object.Equals(this.Item, other.Item)) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GetResponse<T> rhs)) return false;
            return Equals(rhs);
        }

        public override int GetHashCode()
        {
            return HashHelper.GetHashCode(this.Item)
                .CombineHashCode(this.Succeeded);
        }
    }
}
