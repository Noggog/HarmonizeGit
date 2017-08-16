using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit.RepoTranslator
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var translator = new RepoTranslator(args[0].TrimStart('\"').TrimEnd('\"')))
            {
                translator.Translate().Wait();
            }
        }
    }
}
