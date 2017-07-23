using HarmonizeGit.CustomSetup;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
                    var item = new HarmonizeInstallerClass()
                    {
                        TargetDir = @"C:\Program Files (x86)\HarmonizeGit\"
                    };
                    var link = item.CreateLink();
                    //item.DestroyLink();
                    //HarmonizeGitBase harmonize = new HarmonizeGitBase(Environment.CurrentDirectory);
                    //var ret = await harmonize.Handle(args);
                    //Rerouter reroute = new Rerouter();
                    //var status = reroute.Reroute(args);
                    sw.Stop();
                    System.Console.WriteLine($"DONE  {link} Took {sw.ElapsedMilliseconds}ms");
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
