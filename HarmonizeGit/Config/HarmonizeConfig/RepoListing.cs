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
    public class RepoListing : IEquatable<RepoListing>
    {
        public string Nickname;
        public string Sha;
        public string Path;
        public string SuggestedPath;
        public string CommitDate
        {
            get { return this.CommitDateObj.ToString("MM-dd-yyyy HH:mm:ss"); }
            set { this.CommitDateObj = DateTime.Parse(value); }
        }
        public DateTime CommitDateObj;
        public string Description;
        public string Author;
        public string OriginHint;

        public void SetToCommit(Commit commit)
        {
            this.Sha = commit.Sha;
            if (!Settings.Instance.AddMetadataToConfig) return;
            this.Description = commit.MessageShort;
            this.CommitDateObj = commit.Committer.When.DateTime;
            this.Author = commit.Committer.Name;
        }

        public bool Equals(RepoListing other)
        {
            if (other == null) return false;
            if (!object.Equals(this.Nickname, other.Nickname)) return false;
            if (!object.Equals(this.Sha, other.Sha)) return false;
            if (!object.Equals(this.Path, other.Path)) return false;
            if (!object.Equals(this.SuggestedPath, other.SuggestedPath)) return false;
            if (!object.Equals(this.CommitDate, other.CommitDate)) return false;
            if (!object.Equals(this.Description, other.Description)) return false;
            if (!object.Equals(this.Author, other.Author)) return false;
            if (!object.Equals(this.OriginHint, other.OriginHint)) return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is RepoListing listing)) return false;
            return Equals(listing);
        }

        public override int GetHashCode()
        {
            return HashHelper.GetHashCode(
                this.Nickname,
                this.Sha,
                this.Path,
                this.SuggestedPath,
                this.CommitDate,
                this.Description,
                this.Author,
                this.OriginHint);
        }

        public RepoListing GetCopy()
        {
            return new RepoListing()
            {
                Nickname = this.Nickname,
                Sha = this.Sha,
                Path = this.Path,
                SuggestedPath = this.SuggestedPath,
                CommitDate = this.CommitDate,
                Description = this.Description,
                Author = this.Author,
                OriginHint = this.OriginHint,
            };
        }
    }
}
