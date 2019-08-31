using FishingWithGit.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Splat;

namespace HarmonizeGit.GUI
{
    public class SplatLogger : FishingWithGit.Common.ILogger, IEnableLogger
    {
        public bool LogError(string err, string caption, bool showMessageBox)
        {
            this.Log().Error(err);
            return false;
        }

        public bool? LogErrorRetry(string err, string caption, bool showMessageBox)
        {
            this.Log().Error(err);
            return false;
        }

        public bool LogErrorYesNo(string err, string caption, bool showMessageBox)
        {
            this.Log().Error(err);
            return false;
        }

        public void WriteLine(string line, bool error = false, bool? writeToConsole = null)
        {
            if (error)
            {
                this.Log().Error(line);
            }
            else
            {
                this.Log().Info(line);
            }
        }

        public void WriteLine(LogItem item)
        {
            if (item.Error)
            {
                this.Log().Error(item.Message);
            }
            else
            {
                this.Log().Info(item.Message);
            }
        }
    }
}
