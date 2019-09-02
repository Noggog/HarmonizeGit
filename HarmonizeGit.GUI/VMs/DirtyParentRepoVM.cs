using Noggog;
using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;
using System.Diagnostics;
using Splat;

namespace HarmonizeGit.GUI
{
    public class DirtyParentRepoVM : ViewModel, IEnableLogger
    {
        public DirectoryPath Dir { get; }
        public string Name { get; }

        private readonly ObservableAsPropertyHelper<bool> _Dirty;
        public bool Dirty => _Dirty.Value;

        public DirtyParentRepoVM()
        {
        }

        public DirtyParentRepoVM(MainVM mvm, DirectoryPath path)
        {
            this.Dir = path;
            this.Name = path.Name;
            this._Dirty = mvm.DirtyCheckPulse
                .StartWith(Unit.Default)
                .ObserveOn(RxApp.TaskpoolScheduler)
                .SelectTask(async () =>
                {
                    try
                    {
                        if (!path.Exists) return false;
                        using (var repo = new RepoLoader(path.Path))
                        {
                            var errorResp = await HarmonizeFunctionality.IsDirty(
                                path.Path,
                                new ConfigLoader(
                                    path.Path,
                                    repo,
                                    MainVM.HarmonizeLogger),
                                repo,
                                MainVM.HarmonizeLogger);
                            return errorResp.Succeeded;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Log().Error($"Exception while calculating if {this.Name} parent was dirty: {ex}");
                        return false;
                    }
                })
                .DistinctUntilChanged()
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToProperty(this, nameof(Dirty));
        }
    }
}
