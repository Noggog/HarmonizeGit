using DynamicData;
using DynamicData.Binding;
using HarmonizeGit.GUI.Internals;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace HarmonizeGit.GUI
{
    public class MainVM : ViewModel
    {
        // Static constants
        public static MainVM Instance { get; private set; }
        public const string AppName = "Harmonize Git GUI";
        public static readonly string SettingsPath = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData"), $"{AppName}/Settings.xml");

        public Settings Settings { get; }
        public ObservableCollectionExtended<Repository> Repositories { get; } = new ObservableCollectionExtended<Repository>();
        public BitmapFrame Icon => BitmapFrame.Create(new Uri("pack://application:,,,/harmonize_git2.ico", UriKind.RelativeOrAbsolute));

        public IReactiveCommand AddCommand { get; }
        public IReactiveCommand ResyncCommand { get; }

        private ObservableAsPropertyHelper<bool> _Resyncing;
        public bool Resyncing => _Resyncing.Value;

        public readonly IObservable<Unit> SyncPulse = Observable.Interval(TimeSpan.FromSeconds(5), RxApp.MainThreadScheduler)
            .Unit()
            .PublishRefCount();

        private int _PauseSeconds = 5;
        public int PauseSeconds { get => _PauseSeconds; set => this.RaiseAndSetIfChanged(ref _PauseSeconds, value); }

        private bool _Paused;
        public bool Paused { get => _Paused; set => this.RaiseAndSetIfChanged(ref _Paused, value); }

        public MainVM()
        {
        }

        public MainVM(MainWindow window)
        {
            HarmonizeGit.Settings.Instance.LogToFile = false;

            // Create sub objects
            Instance = this;

            this.AddCommand = ReactiveCommand.Create(
                execute: () =>
                {
                    this.Settings.Repositories.Add(new Repository()
                    {
                        Nickname = "New Repo"
                    });
                });
            this.ResyncCommand = ReactiveCommand.Create(ActionExt.Nothing);

            // Save to disk when app closing
            window.Closed += (a, b) =>
            {
                FilePath filePath = new FilePath(SettingsPath);
                filePath.Directory.Create();
                this.Settings.WriteToXml(SettingsPath);
            };

            _Resyncing = this.ResyncCommand.IsExecuting
                .ToProperty(this, nameof(Resyncing));

            this.Settings = Settings.CreateFromXml(SettingsPath);

            // Populate GUI list
            this.Settings.Repositories.Connect()
                .OnItemAdded(r => r.Init(this))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(this.Repositories)
                .Subscribe()
                .DisposeWith(this.CompositeDisposable);

            // Set up pause auto turn off
            Observable.CombineLatest(
                    // Get Pause Timestamp
                    Observable.Merge(
                        // When pause is enabled
                        this.WhenAny(x => x.Paused)
                            .DistinctUntilChanged()
                            .Where(paused => paused)
                            .Select(_ => DateTime.Now),
                        // Or pause seconds changed during pause phase
                        this.WhenAny(x => x.PauseSeconds)
                            .FilterSwitch(this.WhenAny(x => x.Paused))
                            .Select(_ => DateTime.Now)),
                    this.WhenAny(x => x.Paused),
                    this.WhenAny(x => x.PauseSeconds),
                    // Return whether we should fire signal to turn pause off
                    resultSelector: (pauseTime, paused, pauseSeconds) =>
                    {
                        // Don't want to fire anything if in bad state
                        if (!paused) return Observable.Return(false);
                        var endTime = pauseTime.AddSeconds(pauseSeconds);
                        if (endTime < DateTime.Now) Observable.Return(false);
                        // Fire signal after timer
                        return Observable.Timer(endTime)
                            .Select(_ => true);
                    })
                .Switch()
                .Where(s => s) // Only if signal fired
                .Subscribe(pause => this.Paused = false)
                .DisposeWith(this.CompositeDisposable);

            // Set up autosync
            this.SyncPulse
                .StartWith(Unit.Default)
                // Only sync if on and not paused
                .FilterSwitch(
                    Observable.CombineLatest(
                        this.WhenAny(x => x.Settings.AutoSync),
                        this.WhenAny(x => x.Paused),
                        resultSelector: (sync, pause) => sync && !pause)
                    .DistinctUntilChanged())
                .InvokeCommand(this.ResyncCommand)
                .DisposeWith(this.CompositeDisposable);
        }
    }
}
