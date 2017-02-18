using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace HarmonizeGitHooks
{
    public class RepoListing
    {
        public string Nickname;
        public string Path;
        public string Sha;
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
            this.Description = commit.MessageShort;
            this.CommitDateObj = commit.Committer.When.DateTime;
            this.Author = commit.Committer.Name;
        }
    }
}
