using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace HarmonizeGit.GUI
{
    public partial class Repository
    {
        public ICommand OpenRepoFolderDialogCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }

        partial void CustomCtor()
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
            this.RefreshCommand = ReactiveCommand.Create(
                execute: async () =>
                {
                    HarmonizeGit.Settings.Instance.LogToFile = false;
                    HarmonizeGitBase harmonize = new HarmonizeGitBase(this.Path);
                    harmonize.Init();
                    await harmonize.SyncConfigToParentShas();
                });
        }
    }
}
