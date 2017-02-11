using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGitHooks
{
    class Program
    {
        static void Main(string[] args)
        {
            HarmonizeGitBase harmonize = new HarmonizeGitBase();
            harmonize.Handle(args);
        }
    }
}
