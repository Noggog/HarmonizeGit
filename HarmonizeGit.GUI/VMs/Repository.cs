using DynamicData;
using Noggog;
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
using System.IO;
using System.Security.Cryptography;

namespace HarmonizeGit.GUI
{
    public partial class Repository : IEnableLogger
    {
        public MainVM MainVM { get; private set; }

        public IReactiveCommand OpenRepoFolderDialogCommand { get; private set; }
        public IReactiveCommand ResyncCommand { get; private set; }
        public IReactiveCommand DeleteCommand { get; private set; }
        public IReactiveCommand AutoSyncCommand { get; private set; }
        public IReactiveCommand SyncParentReposCommand { get; private set; }
        public IReactiveCommand CloneReposCommand { get; private set; }

        private ObservableAsPropertyHelper<bool> _Resyncing;
        public bool Resyncing => _Resyncing.Value;

        private SourceList<RepoListing> _ParentRepos = new SourceList<RepoListing>();
        public IObservableList<RepoListing> ParentRepos => _ParentRepos;

        private ObservableAsPropertyHelper<bool> _Exists;
        public bool Exists => _Exists.Value;

        private ObservableAsPropertyHelper<bool> _ParentsAllExist;
        public bool ParentsAllExist => _ParentsAllExist.Value;

        public void Init(MainVM mvm)
        {
            this.MainVM = mvm;
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

            this._Resyncing = this.ResyncCommand.IsExecuting
                .ToProperty(this, nameof(Resyncing));
            this.DeleteCommand = ReactiveCommand.Create(
                execute: () =>
                {
                    mvm.Settings.Repositories.Remove(this);
                });
            this.CloneReposCommand = ReactiveCommand.Create(() =>
            {
                this.MainVM.CloningVM.TargetRepository = this;
                this.MainVM.WindowActiveObject = this.MainVM.CloningVM;
            });

            // Exists check
            this._Exists = MainVM.ShortPulse
                .StartWith(Unit.Default)
                .SelectLatest(this.WhenAny(x => x.Path))
                .Select(path => Directory.Exists(path))
                .ToProperty(this, nameof(Exists));

            // All parents exist check
            this._ParentsAllExist = this.ParentRepos.Connect()
                .TransformMany(repoListing =>
                {
                    var parentDir = FishingWithGit.Common.Utility.StandardizePath(repoListing.Path, this.Path);
                    return MainVM.ShortPulse
                        .StartWith(Unit.Default)
                        .Select(_ => Directory.Exists(parentDir))
                        .DistinctUntilChanged();
                })
                .QueryWhenChanged((l) =>
                {
                    return l.All(b => b);
                })
                // Only count parents existing if self exists, too
                .CombineLatest(
                    this.WhenAny(x => x.Exists),
                    resultSelector: (parents, self) => parents && self)
                .ToProperty(this, nameof(ParentsAllExist));

            // Set up autosync
            mvm.SyncPulse
                .StartWith(Unit.Default)
                .FilterSwitch(
                    Observable.CombineLatest(
                        mvm.WhenAny(x => x.Settings.AutoSync),
                        this.WhenAny(x => x.AutoSync),
                        mvm.WhenAny(x => x.Paused),
                        resultSelector: (main, individual, pause) => !main && individual && !pause)
                    // Throttle, as sync/pause changes aren't atomic
                    .Throttle(TimeSpan.FromMilliseconds(50))
                    .DistinctUntilChanged())
                .Merge(mvm.ResyncCommand.IsExecuting
                    .Where(b => b)
                    .Unit())
                .FilterSwitch(this.WhenAny(x => x.ParentsAllExist))
                .InvokeCommand(this.ResyncCommand)
                .DisposeWith(this.CompositeDisposable);

            this.SyncParentReposCommand = ReactiveCommand.CreateFromTask(SyncParentRepos);

            // Compile parent repo list
            mvm.ShortPulse
                .StartWith(Unit.Default)
                .SelectLatest(this.WhenAny(x => x.Path))
                // Only update if config checksum changed
                .Select<string, (string Path, byte[] Checksum)>(path =>
                {
                    if (string.IsNullOrWhiteSpace(path)) return (path, default(byte[]));
                    var configPath = System.IO.Path.Combine(path, Constants.HarmonizeConfigPath);
                    if (!File.Exists(configPath)) return (path, default(byte[]));
                    using (var md5 = MD5.Create())
                    {
                        using (var stream = File.OpenRead(configPath))
                        {
                            var checksum = md5.ComputeHash(stream);
                            return (path, checksum);
                        }
                    }
                })
                .DistinctUntilChanged(
                    keySelector: i => i.Checksum,
                    comparer: new FuncEqualityComparer<byte[]>(equals: (l, r) =>
                    {
                        if (l == null && r == null) return true;
                        if (l == null || r == null) return false;
                        return MemoryExtensions.SequenceEqual(l.AsSpan(), r.AsSpan());
                    }))
                .Select(i => i.Path)
                // Load config and get parent repos
                .Subscribe(path =>
                {
                    try
                    {
                        if (!Directory.Exists(path))
                        {
                            _ParentRepos.Clear();
                            return;
                        }
                        using (var repo = new LibGit2Sharp.Repository(path))
                        {
                            if (!HarmonizeFunctionality.TryLoadConfig(
                                repo,
                                out var config))
                            {
                                this.Log().Error($"Could not load config at path {path} to compile parent repos.");
                                return;
                            }
                            _ParentRepos.Edit(l =>
                            {
                                l.SetTo(config.ParentRepos,
                                    checkEquality: true);
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Log().Error($"Exception while compiling parent repositories for {this.Path}: {ex}");
                        _ParentRepos.Clear();
                    }
                })
                .DisposeWith(this.CompositeDisposable);
        }

        private async Task Resync()
        {
            try
            {
                using (var repoLoader = new RepoLoader(this.Path))
                {
                    if (!HarmonizeFunctionality.TryLoadConfig(
                        repoLoader.GetRepo(this.Path),
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
            catch (Exception ex)
            {
                this.Log().Error($"Exception resyncing {this.Path}: {ex}");
            }
        }

        private async Task SyncParentRepos()
        {
            try
            {
                using (var repoLoader = new RepoLoader(this.Path))
                {
                    if (!HarmonizeFunctionality.TryLoadConfig(
                        repoLoader.GetRepo(this.Path),
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
            catch (Exception ex)
            {
                this.Log().Error($"Exception syncing parent repos {this.Path}: {ex}");
            }
        }
    }
}
