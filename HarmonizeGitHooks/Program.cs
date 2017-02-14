using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGitHooks
{
    class Program
    {
        static int Main(string[] args)
        {
            HarmonizeGitBase harmonize = new HarmonizeGitBase();
            return harmonize.Handle(args) ? 0 : 1;
        }
    }
}
