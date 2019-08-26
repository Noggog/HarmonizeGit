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

namespace HarmonizeGit.GUI
{
    public partial class Repository
    {
        public IReactiveCommand OpenRepoFolderDialogCommand { get; private set; }
        public IReactiveCommand ResyncCommand { get; private set; }
        public IReactiveCommand DeleteCommand { get; private set; }
        public IReactiveCommand AutoSyncCommand { get; private set; }
        public IReactiveCommand SyncParentReposCommand { get; private set; }

        private ObservableAsPropertyHelper<bool> _Resyncing;
        public bool Resyncing => _Resyncing.Value;

        private ObservableAsPropertyHelper<HarmonizeGitBase> _Harmonize;
        public HarmonizeGitBase Harmonize => _Harmonize.Value;

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
            this._Harmonize = this.WhenAny(x => x.Path)
                .StartWith(this.Path)
                .Select(path =>
                {
                    HarmonizeGitBase harmonize = new HarmonizeGitBase(path);
                    harmonize.Init();
                    return harmonize;
                })
                .DisposeWith(this.CompositeDisposable)
                .ToProperty(this, nameof(Harmonize));
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
            if (this.Harmonize == null) return;
            await this.Harmonize.SyncConfigToParentShas();
        }

        public async Task SyncParentRepos()
        {
            if (this.Harmonize == null) return;
            this.Harmonize.SyncParentRepos(HarmonizeConfig.Factory(
                this.Harmonize,
                this.Harmonize.TargetPath,
                this.Harmonize.Repo.Head.Tip));
            await this.Harmonize.SyncConfigToParentShas();
        }
    }
}
