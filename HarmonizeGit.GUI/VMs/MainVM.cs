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
        public IReactiveCommand AutoSyncCommand { get; }

        private ObservableAsPropertyHelper<bool> _Resyncing;
        public bool Resyncing => _Resyncing.Value;

        public readonly IObservable<Unit> SyncPulse = Observable.Interval(TimeSpan.FromSeconds(5), RxApp.MainThreadScheduler)
            .Unit()
            .PublishRefCount();

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
            this.AutoSyncCommand = ReactiveCommand.Create(
                () =>
                {
                    this.Settings.AutoSync = !this.Settings.AutoSync;
                });

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
        }
    }
}
