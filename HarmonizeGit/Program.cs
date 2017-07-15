using System;
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
                    return new Rerouter().Reroute(args);
                }
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine(ex);
                return 1;
            }
        }
    }
}
