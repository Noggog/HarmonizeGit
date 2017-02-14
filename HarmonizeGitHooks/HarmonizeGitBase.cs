using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace HarmonizeGitHooks
{
    class HarmonizeGitBase
    {
        public EventWaitHandle configSyncer = new EventWaitHandle(true, EventResetMode.AutoReset, "GIT_HARMONIZE_CONFIG_SYNCER");
        public const string BranchName = "GitHarmonize";
        public const string HarmonizeConfigPath = ".harmonize";
        public readonly Lazy<HarmonizeConfig> Config;

        public HarmonizeGitBase()
        {
            Config = new Lazy<HarmonizeConfig>(LoadConfig);
        }

        public bool Handle(string[] args)
        {
            if (args.Length < 2) return true;

            TypicalHandlerBase handler;
            switch (args[0])
            {
                case "pre-checkout":
                    handler = new CheckoutHandler(this);
                    break;
                case "post-reset":
                    handler = new ResetHandler(this);
                    break;
                case "pre-commit":
                    handler = new CommitHandler(this);
                    break;
                case "post-status":
                    handler = new StatusHandler(this);
                    break;
                case "post-discard":
                    handler = new DiscardHandler(this);
                    break;
                default:
                    return true;
            }

            List<string> trimmedArgs = new List<string>();
            for (int i = 2; i < args.Length; i++)
            {
                trimmedArgs.Add(args[i]);
            }
            return handler.Handle(trimmedArgs);
        }

        public void WriteLine(string line)
        {
            System.Console.WriteLine(line);
        }

        public void WriteLine(object line)
        {
            System.Console.WriteLine(line);
        }

        public List<RepoListing> GetReposWithUncommittedChanges()
        {
            List<RepoListing> ret = new List<RepoListing>();
            foreach (var repoListing in Config.Value.ParentRepos)
            {
                using (var repo = new Repository(repoListing.Path))
                {
                    if (repo.RetrieveStatus().IsDirty)
                    {
                        ret.Add(repoListing);
                    }
                }
            }
            return ret;
        }

        public void SyncConfigToParentShas()
        {
            var config = this.Config.Value;
            List<RepoListing> changed = new List<RepoListing>();

            foreach (var listing in config.ParentRepos)
            {
                using (var repo = new Repository(listing.Path))
                {
                    if (listing.Sha.Equals(repo.Head.Tip.Sha)) continue;
                    changed.Add(listing);
                    listing.Sha = repo.Head.Tip.Sha;
                }
            }

            if (changed.Count == 0) return;
            this.WriteLine("Updating config as parent repos have changed: ");
            foreach (var change in changed)
            {
                this.WriteLine("  " + change.Nickname);
            }

            string xmlStr;
            XmlSerializer xsSubmit = new XmlSerializer(typeof(HarmonizeConfig));
            var settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.OmitXmlDeclaration = true;
            var emptyNs = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            using (var sw = new StringWriter())
            {
                using (XmlWriter writer = XmlWriter.Create(sw, settings))
                {
                    xsSubmit.Serialize(writer, config, emptyNs);
                    xmlStr = sw.ToString();
                }
            }

            configSyncer.WaitOne();
            try
            {
                File.WriteAllText(HarmonizeConfigPath, xmlStr);
            }
            finally
            {
                configSyncer.Set();
            }
        }

        private HarmonizeConfig LoadConfig()
        {
            configSyncer.WaitOne();
            try
            {
                using (var stream = new FileStream(HarmonizeConfigPath, FileMode.Open, FileAccess.Read))
                {
                    return HarmonizeConfig.Factory(stream);
                }
            }
            finally
            {
                configSyncer.Set();
            }
        }

        public void SyncParentRepos()
        {
            SyncParentRepos(this.Config.Value);
        }

        public void SyncParentRepos(HarmonizeConfig config)
        {
            foreach (var listing in config.ParentRepos)
            {
                this.WriteLine($"Processing {listing.Nickname} at path {listing.Path}. Trying to check out an existing branch at {listing.Sha}.");

                using (var repo = new Repository(listing.Path))
                {
                    var existingBranch = repo.Branches
                        .Where((b) => b.Tip.Sha.Equals(listing.Sha))
                        .OrderBy((b) => b.FriendlyName.Contains("GitHarmonize") ? 0 : 1)
                        .FirstOrDefault();
                    if (existingBranch != null)
                    {
                        this.WriteLine($"Checking out existing branch {listing.Nickname}:{existingBranch.FriendlyName}.");
                        LibGit2Sharp.Commands.Checkout(repo, existingBranch.FriendlyName);
                        return;
                    }
                    this.WriteLine("No branch found.  Allocating a Harmonize branch.");
                    for (int i = 0; i < 100; i++)
                    {
                        var branchName = BranchName + (i == 0 ? "" : i.ToString());
                        var harmonizeBranch = repo.Branches[branchName];
                        if (harmonizeBranch == null)
                        { // Create new branch
                            this.WriteLine($"Creating {listing.Nickname}:{branchName}.");
                            var branch = repo.CreateBranch(branchName, listing.Sha);
                            Commands.Checkout(repo, branch);
                            return;
                        }
                        else if (IsLoneTip(repo, harmonizeBranch, harmonizeBranch.Tip.Sha))
                        {
                            this.WriteLine(harmonizeBranch.FriendlyName + " was unsafe to move.");
                            continue;
                        }
                        else
                        {
                            this.WriteLine($"Moving {listing.Nickname}:{harmonizeBranch.FriendlyName} to target commit.");
                            Commands.Checkout(repo, harmonizeBranch);
                            repo.Reset(ResetMode.Hard, listing.Sha);
                            return;
                        }
                    }
                }
                throw new NotImplementedException("Delete some branches.  You have over 100.");
            }
        }

        public void SyncParentReposToSha(string targetCommitSha)
        {
            HarmonizeConfig targetConfig;
            using (var repo = new Repository("."))
            {
                var targetCommit = repo.Lookup<Commit>(targetCommitSha);
                if (targetCommit == null)
                {
                    throw new ArgumentException("Target commit does not exist. " + targetCommitSha);
                }

                var entry = targetCommit[HarmonizeConfigPath];
                var blob = entry?.Target as Blob;
                if (blob == null)
                {
                    this.WriteLine("No harmonize config at target commit.  Exiting without syncing.");
                    return;
                }

                var contentStream = blob.GetContentStream();
                using (var tr = new StreamReader(contentStream, Encoding.UTF8))
                {
                    targetConfig = HarmonizeConfig.Factory(tr.BaseStream);
                }
            }
            SyncParentRepos(targetConfig);
        }

        private bool IsLoneTip(Repository repo, Branch targetBranch, string sha)
        {
            foreach (var branch in ListBranchesContaininingCommit(repo, sha))
            {
                if (branch.Equals(targetBranch)) continue;
                return false;
            }
            return true;
        }

        private IEnumerable<Branch> ListBranchesContaininingCommit(Repository repo, string commitSha)
        {
            foreach (var branch in repo.Branches)
            {
                var commits = repo.Commits.QueryBy(
                    new CommitFilter()
                    {
                        IncludeReachableFrom = branch.Tip.Sha
                    })
                    .Where(c => c.Sha == commitSha);

                if (!commits.Any())
                {
                    continue;
                }

                yield return branch;
            }
        }
    }
}
