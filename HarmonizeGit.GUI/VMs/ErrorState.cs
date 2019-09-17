using Noggog.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGit.GUI
{
    public enum ErrorLevel
    {
        Warning,
        Error
    }

    public class ErrorState : ViewModel
    {
        private ErrorLevel _Level;
        public ErrorLevel Level { get => _Level; set => this.RaiseAndSetIfChanged(ref _Level, value); }

        private string _Message;
        public string Message { get => _Message; set => this.RaiseAndSetIfChanged(ref _Message, value); }
    }
}
