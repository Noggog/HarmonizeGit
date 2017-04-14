using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit
{
    class Program
    {
        static int Main(string[] args)
        {
            if (!Properties.Settings.Default.Enabled) return 0;
            if (!Properties.Settings.Default.Reroute)
            {
                DirectoryInfo dir = new DirectoryInfo(".");
                HarmonizeGitBase harmonize = new HarmonizeGitBase(dir.FullName);
                return harmonize.Handle(args).Result ? 0 : 1;
            }
            else
            {
                var pathing = PathingConfig.Factory(".");
                if (string.IsNullOrWhiteSpace(pathing.ReroutePathing))
                {
                    FileInfo info = new FileInfo("./" + HarmonizeGitBase.HarmonizePathingPath);
                    pathing.Write(".");
                    System.Console.Error.WriteLine($"No routing path specified.  Add it here: {info.FullName}");
                    return -1;
                }
                ProcessStartInfo startInfo = new ProcessStartInfo(
                    pathing.ReroutePathing,
                    string.Join(" ", args))
                {
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                using (Process proc = Process.Start(startInfo))
                {
                    proc.WaitForExit();
                    System.Console.WriteLine(proc.StandardOutput.ReadToEnd());
                    System.Console.Error.WriteLine(proc.StandardError.ReadToEnd());
                    return proc.ExitCode;
                }
            }
        }
    }
}
