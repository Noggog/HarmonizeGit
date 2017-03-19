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
            if (string.IsNullOrWhiteSpace(Properties.Settings.Default.RoutePath))
            {
                DirectoryInfo dir = new DirectoryInfo(".");
                HarmonizeGitBase harmonize = new HarmonizeGitBase(dir.FullName)
                {
                    FileLock = Properties.Settings.Default.Lock
                };
                return harmonize.Handle(args).Result ? 0 : 1;
            }
            else
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(
                    Properties.Settings.Default.RoutePath,
                    string.Join(" ", args));
                startInfo.CreateNoWindow = true;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
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
