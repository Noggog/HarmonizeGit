using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HarmonizeGit
{
    public class FileLockCheckout : IDisposable
    {
        EventWaitHandle handle;

        public FileLockCheckout()
        {
        }

        public FileLockCheckout(EventWaitHandle handle)
        {
            this.handle = handle;
            this.handle.WaitOne();
        }

        public void Dispose()
        {
            this.handle?.Set();
        }
    }
}
