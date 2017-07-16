using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit.Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                try
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    HarmonizeGitBase harmonize = new HarmonizeGitBase(Environment.CurrentDirectory);
                    var ret = await harmonize.Handle(args);
                    Rerouter reroute = new Rerouter();
                    var status = reroute.Reroute(args);
                    sw.Stop();
                    System.Console.WriteLine($"DONE   Took {sw.ElapsedMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine(ex.ToString());
                }
            });
            System.Console.ReadLine();
        }
    }
}
