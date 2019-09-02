using DynamicData;
using DynamicData.Binding;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Splat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace HarmonizeGit.GUI
{
    public class CloningVM : ViewModel
    {
        public MainVM MVM { get; }

        private Repository _TargetRepository;
        public Repository TargetRepository { get => _TargetRepository; set => this.RaiseAndSetIfReferenceChanged(ref _TargetRepository, value); }

        public IReactiveCommand GoBackCommand { get; }
        public IReactiveCommand CloneAllCommand { get; }

        public ObservableCollectionExtended<ParentRepoCloningVM> ParentRepositories { get; } = new ObservableCollectionExtended<ParentRepoCloningVM>();

        private IObservableList<ParentRepoCloningVM> ClonableParentRepositories { get; }

        public CloningVM()
        {
        }

        public CloningVM(MainVM mvm)
        {
            this.MVM = mvm;
            this.GoBackCommand = ReactiveCommand.Create(() => this.MVM.WindowActiveObject = this.MVM);
            var clonableParentRepositories = this.WhenAny(x => x.TargetRepository)
                .Select(repo =>
                {
                    if (repo == null) return Observable.Empty<IChangeSet<ParentRepoCloningVM>>();
                    return repo.ParentRepos.Connect()
                        .Transform(listing => new ParentRepoCloningVM(listing, repo));
                })
                .Switch()
                .DisposeMany()
                .Bind(ParentRepositories)
                .FilterOnObservable(x => x.CloneCommand.CanExecute)
                .AsObservableList();
            this.CloneAllCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await Task.WhenAll(clonableParentRepositories
                    .Select(r => r.Clone(CancellationToken.None)));
            });
        }

        public class ParentRepoCloningVM : ViewModel, IEnableLogger
        {
            public Repository Repo { get; }

            private string _Nickname;
            public string Nickname { get => _Nickname; set => this.RaiseAndSetIfChanged(ref _Nickname, value); }

            private string _Origin;
            public string Origin { get => _Origin; set => this.RaiseAndSetIfChanged(ref _Origin, value); }
            
            public string TargetPath { get; }

            public IReactiveCommand CloneCommand { get; }

            private ErrorResponse _Error = ErrorResponse.Success;
            public ErrorResponse Error { get => _Error; set => this.RaiseAndSetIfChanged(ref _Error, value); }

            private readonly ObservableAsPropertyHelper<bool> _Exists;
            public bool Exists => _Exists.Value;

            private readonly Subject<Unit> _ClonedSignal = new Subject<Unit>();
            private readonly Subject<(int Phase, int Progress, int MaxProgress, bool Indefinite)> _ProgressSubject = new Subject<(int Phase, int Progress, int MaxProgress, bool Indefinite)>();

            public ParentRepoCloningVM(RepoListing listing, Repository repo)
            {
                this.Repo = repo;
                this.Nickname = listing.Nickname;
                this.Origin = listing.OriginHint;
                this.TargetPath = FishingWithGit.Common.Utility.StandardizePath(listing.Path, repo.Path);

                // Exists check
                this._Exists = repo.MainVM.ShortPulse
                    .StartWith(Unit.Default)
                    .Merge(_ClonedSignal)
                    .Select(_ => Directory.Exists(this.TargetPath))
                    .ToProperty(this, nameof(Exists));

                this.CloneCommand = ReactiveCommand.CreateFromTask(
                    canExecute: Observable.CombineLatest(
                        this.WhenAny(x => x.Exists),
                        this.WhenAny(x => x.Origin),
                        resultSelector: (exist, origin) =>
                        {
                            return !exist && !string.IsNullOrWhiteSpace(origin);
                        }),
                    execute: Clone);
            }

            public async Task Clone(CancellationToken cancel)
            {
                try
                {
                    this._ProgressSubject.OnNext((0, 0, 1, true));
                    if (Directory.Exists(this.TargetPath))
                    {
                        this.Error = ErrorResponse.Fail("Target repo directory already exists.");
                        return;
                    }
                    LibGit2Sharp.CloneOptions cloneOptions = new LibGit2Sharp.CloneOptions()
                    {
                        OnProgress = (prog) =>
                        {
                            return !cancel.IsCancellationRequested;
                        },
                        OnTransferProgress = (prog) =>
                        {
                            this._ProgressSubject.OnNext((0, prog.IndexedObjects, prog.TotalObjects, false));
                            return !cancel.IsCancellationRequested;
                        },
                        OnCheckoutProgress = (path, completed, total) =>
                        {
                            this._ProgressSubject.OnNext((0, completed, total, false));
                        },
                    };
                    await Task.Run(() =>
                    {
                        LibGit2Sharp.Repository.Clone(this.Origin, this.TargetPath, cloneOptions);
                    });
                    _ClonedSignal.OnNext(Unit.Default);
                }
                catch (Exception ex)
                {
                    this.Error = ErrorResponse.Fail($"Error while cloning: {ex.Message}");
                    this.Log().Warn($"Error while cloning: {ex}");
                }
            }
        }
    }
}
