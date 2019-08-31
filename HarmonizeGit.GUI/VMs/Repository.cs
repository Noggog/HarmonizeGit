using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using Splat;

namespace HarmonizeGit.GUI
{
    public partial class Repository : IEnableLogger
    {
        public IReactiveCommand OpenRepoFolderDialogCommand { get; private set; }
        public IReactiveCommand ResyncCommand { get; private set; }
        public IReactiveCommand DeleteCommand { get; private set; }
        public IReactiveCommand AutoSyncCommand { get; private set; }
        public IReactiveCommand SyncParentReposCommand { get; private set; }

        private ObservableAsPropertyHelper<bool> _Resyncing;
        public bool Resyncing => _Resyncing.Value;

        public void Init(MainVM mvm)
        {
            this.OpenRepoFolderDialogCommand = ReactiveCommand.Create(
                execute: () =>
                {
                    using (var fbd = new FolderBrowserDialog())
                    {
                        fbd.SelectedPath = MainVM.Instance.Settings.LastReferencedDirectory;
                        DialogResult result = fbd.ShowDialog();

                        if (result == DialogResult.OK)
                        {
                            this.Path = fbd.SelectedPath;
                            MainVM.Instance.Settings.LastReferencedDirectory = fbd.SelectedPath;
                        }
                    }
                });
            this.ResyncCommand = ReactiveCommand.CreateFromTask(Resync);
            this.AutoSyncCommand = ReactiveCommand.Create(() =>
            {
                this.AutoSync = !this.AutoSync;
            });

            _Resyncing = this.ResyncCommand.IsExecuting
                .ToProperty(this, nameof(Resyncing));
            this.DeleteCommand = ReactiveCommand.Create(
                execute: () =>
                {
                    mvm.Settings.Repositories.Remove(this);
                });

            // Set up autosync
            mvm.SyncPulse
                .StartWith(Unit.Default)
                .FilterSwitch(
                    Observable.CombineLatest(
                        mvm.WhenAny(x => x.Settings.AutoSync),
                        this.WhenAny(x => x.AutoSync),
                        resultSelector: (main, individual) => !main && individual)
                    .DistinctUntilChanged())
                .Merge(mvm.ResyncCommand.IsExecuting
                    .Where(b => b)
                    .Unit())
                .InvokeCommand(this.ResyncCommand)
                .DisposeWith(this.CompositeDisposable);

            this.SyncParentReposCommand = ReactiveCommand.CreateFromTask(SyncParentRepos);
        }

        public async Task Resync()
        {
            using (var repoLoader = new RepoLoader(this.Path))
            {
                if (!HarmonizeFunctionality.TryLoadConfig(
                    this.Path,
                    repoLoader,
                    out var config))
                {
                    // ToDo
                    // Display errors
                    this.Log().Warn("Could not create harmonize config to resync.");
                    return;
                }
                await HarmonizeFunctionality.SyncAndWriteConfig(config, this.Path, repoLoader, MainVM.HarmonizeLogger);
            }
        }

        public async Task SyncParentRepos()
        {
            using (var repoLoader = new RepoLoader(this.Path))
            {
                if (!HarmonizeFunctionality.TryLoadConfig(
                    this.Path,
                    repoLoader,
                    out var config))
                {
                    // ToDo
                    // Display errors
                    this.Log().Warn("Could not create harmonize config to sync parent repos.");
                    return;
                }
                foreach (var listing in config.ParentRepos)
                {
                    HarmonizeFunctionality.SyncParentRepo(
                        listing,
                        MainVM.HarmonizeLogger,
                        repoLoader);
                }
                await HarmonizeFunctionality.SyncAndWriteConfig(config, this.Path, repoLoader, MainVM.HarmonizeLogger);
            }
        }
    }
}
