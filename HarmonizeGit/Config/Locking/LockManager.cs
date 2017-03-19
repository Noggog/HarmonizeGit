using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HarmonizeGit
{
    public class LockManager
    {
        private Dictionary<LockType, Dictionary<string, EventWaitHandle>> tracker = new Dictionary<LockType, Dictionary<string, EventWaitHandle>>();
        private HarmonizeGitBase harmonize;

        public LockManager(HarmonizeGitBase harmonize)
        {
            this.harmonize = harmonize;
        }

        public FileLockCheckout GetLock(LockType type, string pathToRepo)
        {
            if (!this.harmonize.FileLock) return new FileLockCheckout();

            if (!tracker.TryGetValue(type, out Dictionary<string, EventWaitHandle> dict))
            {
                dict = new Dictionary<string, EventWaitHandle>();
                tracker[type] = dict;
            }
            var lower = pathToRepo.ToLower();
            if (!dict.TryGetValue(lower, out EventWaitHandle handle))
            {
                DirectoryInfo dir = new DirectoryInfo(pathToRepo);
                handle = new EventWaitHandle(true, EventResetMode.AutoReset, $"GIT_HARMONIZE_{type.ToString()}_{dir.Name}");
                dict[lower] = handle;
            }

            return new FileLockCheckout(handle);
        }
    }
}
