using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit
{
    public class Rerouter
    {
        public int Reroute(string[] args)
        {
            if (!TryGetReroutePath(out var reroutePath)) return -1;

            ProcessStartInfo startInfo = new ProcessStartInfo(
                reroutePath,
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

        public bool TryGetReroutePath(out string reroutePath)
        {
            FileInfo pathingFileLocation = new FileInfo("./" + Constants.HarmonizePathingPath);
            PathingConfig pathing;
            try
            {
                pathing = PathingConfig.Factory(".");
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"Error loading routing path at: {pathingFileLocation.FullName}. " + ex);
                reroutePath = null;
                return false;
            }
            if (!pathingFileLocation.Exists)
            {
                pathing.WriteToPath(".");
            }
            if (string.IsNullOrWhiteSpace(pathing.ReroutePathing))
            {
                // Try typical program file paths
                FileInfo harmonizeExe = new FileInfo(
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                        Constants.HarmonizeEXEPath));
                if (harmonizeExe.Exists)
                {
                    reroutePath = harmonizeExe.FullName;
                    return true;
                }
                harmonizeExe = new FileInfo(
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                        Constants.HarmonizeEXEPath));
                if (harmonizeExe.Exists)
                {
                    reroutePath = harmonizeExe.FullName;
                    return true;
                }

                System.Console.Error.WriteLine($"No routing path specified.  Add it here: {pathingFileLocation.FullName}");
                reroutePath = null;
                return false;
            }

            reroutePath = pathing.ReroutePathing.Trim();

            // Check route file exists
            try
            {
                FileInfo rerouteFile = new FileInfo(reroutePath);
                if (!rerouteFile.Exists)
                {
                    System.Console.Error.WriteLine($"Routing path did not lead to an exe.  Fix it here: {pathingFileLocation.FullName}");
                    reroutePath = null;
                    return false;
                }
                if (rerouteFile.FullName.Equals(System.Reflection.Assembly.GetEntryAssembly().Location))
                {
                    System.Console.Error.WriteLine($"Routing path lead to a loop.  Someone needs to turn off routing.");
                    reroutePath = null;
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"Routing path was invalid. ({reroutePath.Trim()})  Fix it here: {pathingFileLocation.FullName}  " + ex);
                reroutePath = null;
                return false;
            }
            return true;
        }
    }
}
