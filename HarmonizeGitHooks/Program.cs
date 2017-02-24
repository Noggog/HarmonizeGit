using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGitHooks
{
    class Program
    {
        static int Main(string[] args)
        {
            if (!Properties.Settings.Default.Enabled) return 0;
            if (string.IsNullOrWhiteSpace(Properties.Settings.Default.RoutePath))
            {
                HarmonizeGitBase harmonize = new HarmonizeGitBase();
                return harmonize.Handle(args) ? 0 : 1;
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
