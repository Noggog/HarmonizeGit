using HarmonizeGitHooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGitTester
{
    class Program
    {
        static void Main(string[] args)
        {
            var harmonize = new HarmonizeGitBase("C:\\Users\\Noggog\\Documents\\DynamicLeveledLists");
            harmonize.ChildLoader.CheckAndSeed(
                "C:\\Users\\Noggog\\Documents\\Noggolloquy",
                "C:\\Users\\Noggog\\Documents\\DynamicLeveledLists").Wait();
        }
    }
}
