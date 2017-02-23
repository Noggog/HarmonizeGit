using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace HarmonizeGitHooks
{
    public class RepoListing
    {
        public string Nickname;
        public string Sha;
        [NonSerialized]
        public string Path;
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

        public override bool Equals(object obj)
        {
            RepoListing rhs = obj as RepoListing;
            if (rhs == null) return false;
            if (!object.Equals(Nickname, rhs.Nickname)) return false;
            if (!object.Equals(Sha, rhs.Sha)) return false;
            if (!object.Equals(CommitDateObj, rhs.CommitDateObj)) return false;
            if (!object.Equals(Description, rhs.Description)) return false;
            if (!object.Equals(Author, rhs.Author)) return false;
            return true;
        }
    }
}
