using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace HarmonizeGit
{
    public class RepoListing
    {
        public string Nickname;
        public string Sha;
        [XmlIgnore]
        public string Path;
        public string SuggestedPath;
        public string CommitDate
        {
            get { return this.CommitDateObj.ToString("MM-dd-yyyy HH:mm:ss"); }
            set { this.CommitDateObj = DateTime.Parse(value); }
        }
        [XmlIgnore]
        public DateTime CommitDateObj;
        public string Description;
        public string Author;

        public void SetToCommit(Commit commit)
        {
            this.Sha = commit.Sha;
            if (!Properties.Settings.Default.AddMetadataToConfig) return;
            this.Description = commit.MessageShort;
            this.CommitDateObj = commit.Committer.When.DateTime;
            this.Author = commit.Committer.Name;
        }
    }
}
