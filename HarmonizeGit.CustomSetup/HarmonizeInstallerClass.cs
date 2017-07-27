using FishingWithGit.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HarmonizeGit.CustomSetup
{
    [RunInstaller(true)]
    public class HarmonizeInstallerClass : Installer
    {
        enum SymbolicLink
        {
            File = 0,
            Directory = 1
        }

        [DllImport("kernel32.dll")]
        static extern bool CreateSymbolicLink(
        string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

        [DllImport("kernel32.dll")]
        static extern uint GetLastError();

        private string _targetDir;
        public string TargetDir
        {
            get => _targetDir ?? this.Context.Parameters["targetdir"].TrimEnd('\\') + '\\';
            set => _targetDir = value;
        }

        private DirectoryInfo _MassHookDir;


        public override void Commit(IDictionary savedState)
        {
            base.Commit(savedState);
            CreateLink();
        }

        public override void Install(IDictionary stateSaver)
        {
            base.Install(stateSaver);
            CreateLink();
        }

        public override void Rollback(IDictionary savedState)
        {
            base.Rollback(savedState);
            DestroyLink();
        }

        public override void Uninstall(IDictionary savedState)
        {
            base.Uninstall(savedState);
            DestroyLink();
        }

        public bool GetMassHookDir(out DirectoryInfo dir)
        {
            if (_MassHookDir != null)
            {
                dir = _MassHookDir;
                return true;
            }
            if (!GetFishingInstall(out var fishingPath))
            {
                dir = null;
                return false;
            }
            dir = new DirectoryInfo(Path.Combine(fishingPath, Constants.MASS_HOOK_FOLDER_NAME, "HarmonizeGit\\"));
            _MassHookDir = dir;
            return true;
        }

        public bool CreateLink()
        {
            DestroyLink();
            if (!GetMassHookDir(out var massHookDir)) return false;
            if (!massHookDir.Parent.Exists)
            {
                massHookDir.Parent.Create();
            }
            if (!CreateSymbolicLink(massHookDir.FullName.TrimEnd('\\'), TargetDir.TrimEnd('\\'), SymbolicLink.Directory))
            {
                var errText = $"Cannot make symbolic link from {TargetDir.TrimEnd('\\')} to {massHookDir.FullName.TrimEnd('\\')}.   {GetLastError()}";
                var result = MessageBox.Show(errText, "Cannot make symbolic link", MessageBoxButtons.AbortRetryIgnore);
                switch (result)
                {
                    case DialogResult.Retry:
                        return CreateLink();
                    case DialogResult.Ignore:
                        return true;
                    case DialogResult.Abort:
                    default:
                        throw new InstallException(errText);
                }
                throw new InstallException(errText);
            }
            return true;
        }

        public void DestroyLink()
        {
            if (!GetMassHookDir(out var massHookDir)) return;
            if (!massHookDir.Exists) return;
            Directory.Delete(massHookDir.FullName);
        }

        public bool GetFishingInstall(out string fishingPath)
        {
            string pathStr = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine);
            string[] paths = pathStr.Split(';');
            foreach (var path in paths)
            {
                try
                {
                    var dir = new DirectoryInfo(path);
                    if (!dir.Name.Equals("cmd")) continue;
                    if (!dir.Exists) continue;
                    foreach (var file in dir.EnumerateFiles())
                    {
                        if (!file.Name.Equals("git.exe")) continue;
                        if (!Utility.TestIfFishingEXE(file.FullName)) continue;

                        fishingPath = dir.Parent.FullName;
                        return true;
                    }
                }
                catch (ArgumentException)
                {
                }
            }
            fishingPath = null;
            return false;
        }
    }
}
