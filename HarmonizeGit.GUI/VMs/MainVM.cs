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

        public ICommand AddCommand { get; }

        public MainVM()
        {
        }

        public MainVM(MainWindow window)
        {
            // Create sub objects
            Instance = this;
            this.Settings = Settings.CreateFromXml(SettingsPath);

            this.AddCommand = ReactiveCommand.Create(
                execute: () =>
                {
                    this.Settings.Repositories.Add(new Repository()
                    {
                        Nickname = "New Repo"
                    });
                });

            // Populate GUI list
            this.Settings.Repositories.Connect()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(this.Repositories)
                .Subscribe();

            // Save to disk when app closing
            window.Closed += (a, b) =>
            {
                FilePath filePath = new FilePath(SettingsPath);
                filePath.Directory.Create();
                this.Settings.WriteToXml(SettingsPath);
            };
        }
    }
}
