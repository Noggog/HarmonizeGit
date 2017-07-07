using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HarmonizeGit
{
    public static class LockManager
    {
        private static Dictionary<LockType, Dictionary<string, EventWaitHandle>> tracker = new Dictionary<LockType, Dictionary<string, EventWaitHandle>>();
        
        public static FileLockCheckout GetLock(LockType type, string pathToRepo)
        {
            if (!Settings.Instance.Lock) return new FileLockCheckout();

            lock (tracker)
            {
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
}
