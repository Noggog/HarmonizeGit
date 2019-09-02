using Noggog.WPF;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit.GUI
{
    public class CloningVM : ViewModel
    {
        public MainVM MVM { get; }

        private Repository _TargetRepository;
        public Repository TargetRepository { get => _TargetRepository; set => this.RaiseAndSetIfReferenceChanged(ref _TargetRepository, value); }

        public IReactiveCommand GoBackCommand { get; }

        public CloningVM()
        {
        }

        public CloningVM(MainVM mvm)
        {
            this.MVM = mvm;
            this.GoBackCommand = ReactiveCommand.Create(() => this.MVM.WindowActiveObject = this.MVM);
        }
    }
}
