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
using Splat;

namespace HarmonizeGit.GUI
{
    public class MainVM : ViewModel, IEnableLogger
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
        public IReactiveCommand PauseSecondsCommand { get; }

        private ObservableAsPropertyHelper<bool> _Resyncing;
        public bool Resyncing => _Resyncing.Value;

        public readonly IObservable<Unit> SyncPulse = Observable.Interval(TimeSpan.FromSeconds(5), RxApp.MainThreadScheduler)
            .Unit()
            .PublishRefCount();

        private bool _Paused;
        public bool Paused { get => _Paused; set => this.RaiseAndSetIfChanged(ref _Paused, value); }

        private readonly ObservableAsPropertyHelper<double> _PauseProgress;
        public double PauseProgress => _PauseProgress.Value;

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
            this.PauseSecondsCommand = ReactiveCommand.Create<int>((param) => this.Settings.PauseSeconds = param);

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
            var pauseProgress = Observable.CombineLatest(
                    // Get Pause Timestamp
                    Observable.Merge(
                        // When pause is enabled
                        this.WhenAny(x => x.Paused)
                            .DistinctUntilChanged()
                            .Where(paused => paused)
                            .Select(_ => DateTime.Now),
                        // Or pause seconds changed during pause phase
                        this.WhenAny(x => x.Settings.PauseSeconds)
                            .FilterSwitch(this.WhenAny(x => x.Paused))
                            .Select(_ => DateTime.Now)),
                    this.WhenAny(x => x.Paused),
                    this.WhenAny(x => x.Settings.PauseSeconds),
                    // Return whether we should fire signal to turn pause off
                    resultSelector: (pauseTime, paused, pauseSeconds) =>
                    {
                        // Don't want to fire anything if in bad state
                        if (!paused) return Observable.Return(Percent.Zero);
                        var endTime = pauseTime.AddSeconds(pauseSeconds);
                        var startTime = DateTime.Now;
                        if (endTime < DateTime.Now) return Observable.Return(Percent.Zero);
                        return ObservableExt.ProgressInterval(startTime, endTime, TimeSpan.FromMilliseconds(50));
                    })
                .Switch()
                .PublishRefCount();
            pauseProgress
                .Where(s => s == Percent.One) // Only if signal fired
                .Subscribe(pause => this.Paused = false)
                .DisposeWith(this.CompositeDisposable);

            // Set up pause progress bar
            this._PauseProgress = pauseProgress
                .Select(p => p.Value)
                .ToProperty(this, nameof(PauseProgress));

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
