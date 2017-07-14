﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                if (!Settings.Instance.Enabled) return 0;
                if (!Settings.Instance.Reroute)
                {
                    DirectoryInfo dir = new DirectoryInfo(".");
                    HarmonizeGitBase harmonize = new HarmonizeGitBase(dir.FullName);
                    return Task.Run(async () =>
                    {
                        var task = harmonize.Handle(args);
                        var doneTask = await Task.WhenAny(task, Task.Delay(Settings.Instance.TimeoutMS));
                        if (task == doneTask)
                        {
                            return await task;
                        }
                        if (!harmonize.Silent)
                        {
                            System.Console.Error.WriteLine("Harmonize timed out.");
                        }
                        return false;
                    }).Result ? 0 : 1;
                }
                else
                {
                    return Reroute(args);
                }
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine(ex);
                return 1;
            }
        }

        public static int Reroute(string[] args)
        {
            FileInfo pathingFileLocation = new FileInfo("./" + HarmonizeGitBase.HarmonizePathingPath);
            PathingConfig pathing;
            try
            {
                pathing = PathingConfig.Factory(".");
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"Error loading routing path at: {pathingFileLocation.FullName}. " + ex);
                return -1;
            }
            if (!pathingFileLocation.Exists)
            {
                pathing.WriteToPath(".");
            }
            if (string.IsNullOrWhiteSpace(pathing.ReroutePathing))
            {
                System.Console.Error.WriteLine($"No routing path specified.  Add it here: {pathingFileLocation.FullName}");
                return -1;
            }
            try
            {
                FileInfo reroutePath = new FileInfo(pathing.ReroutePathing.Trim());
                if (!reroutePath.Exists)
                {
                    System.Console.Error.WriteLine($"Routing path did not lead to an exe.  Fix it here: {pathingFileLocation.FullName}");
                    return -1;
                }
                if (reroutePath.FullName.Equals(System.Reflection.Assembly.GetEntryAssembly().Location))
                {
                    System.Console.Error.WriteLine($"Routing path lead to a loop.  Someone needs to turn off routing.");
                    return -1;
                }
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine($"Routing path was invalid. ({pathing.ReroutePathing.Trim()})  Fix it here: {pathingFileLocation.FullName}  " + ex);
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
