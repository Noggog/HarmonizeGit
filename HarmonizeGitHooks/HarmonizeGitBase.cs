﻿using HarmonizeGitHooks.MetaData;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace HarmonizeGitHooks
{
    class HarmonizeGitBase
    {
        public const string BranchName = "GitHarmonize";
        public readonly Lazy<HarmonizeConfig> Config = new Lazy<HarmonizeConfig>(
            () =>
            {
                return HarmonizeConfig.Factory(".harmonize");
            });

        public void Handle(string[] args)
        {
            if (args.Length < 2) return;

            TypicalHandlerBase handler;
            switch (args[0])
            {
                case "pre-checkout":
                    handler = new CheckoutHandler(this);
                    break;
                case "commit-msg":
                    handler = new CommitMsgHandler(this);
                    break;
                case "pre-reset":
                    handler = new ResetHandler(this);
                    break;
                default:
                    return;
            }

            List<string> trimmedArgs = new List<string>();
            for (int i = 2; i < args.Length; i++)
            {
                trimmedArgs.Add(args[i]);
            }
            handler.Handle(trimmedArgs);
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

        public bool GetShasForParentRepoCommits(string commitMsg, out List<Tuple<RepoListing, string>> list)
        {
            var index = commitMsg.IndexOf("<HarmonizeGitMeta>");
            if (index == -1)
            {
                list = null;
                this.WriteLine("No HarmonizeGit meta info found in commit message.");
                return false;
            }
            var dupIndex = commitMsg.LastIndexOf("<HarmonizeGitMeta>");
            if (index != dupIndex)
            {
                list = null;
                this.WriteLine("Multiple HarmonizeGit metadata found in commit message.");
                return false;
            }
            var endNode = "</HarmonizeGitMeta>";
            var endIndex = commitMsg.IndexOf(endNode);
            if (endIndex == -1)
            {
                list = null;
                this.WriteLine("No end HarmonizeGit metadata xml node.");
                return false;
            }
            if (index > endIndex)
            {
                list = null;
                this.WriteLine("HarmonizeGit metadata end node came before start node.");
                return false;
            }
            var metaData = commitMsg.Substring(index, endIndex + endNode.Length - index);

            HarmonizeGitMeta meta;
            using (StringReader read = new StringReader(metaData))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(HarmonizeGitMeta));
                using (XmlReader reader = new XmlTextReader(read))
                {
                    meta = (HarmonizeGitMeta)serializer.Deserialize(reader);
                }
            }

            list = new List<Tuple<RepoListing, string>>();
            foreach (var r in meta.Refs)
            {
                foreach (var listing in this.Config.Value.ParentRepos)
                {
                    if (listing.Nickname.Equals(r.Nickname))
                    {
                        list.Add(
                            new Tuple<RepoListing, string>(
                                listing,
                                r.Sha));
                    }
                }
            }

            return true;
        }

        public void SyncParentReposToSha(string curRepoSha)
        {
            string commitMsg;
            using (var repo = new Repository("."))
            {
                var targetCommit = repo.Lookup<Commit>(curRepoSha);
                if (targetCommit == null)
                {
                    throw new ArgumentException("Target commit does not exist. " + curRepoSha);
                }

                commitMsg = targetCommit.Message;
            }


            List<Tuple<RepoListing, string>> list;
            if (!this.GetShasForParentRepoCommits(commitMsg, out list))
            {
                this.WriteLine("Error getting metadata.");
            }

            foreach (var listing in list)
            {
                this.WriteLine($"Processing {listing.Item1.Nickname} at path {listing.Item1.Path}. Trying to check out an existing branch at {listing.Item2}.");

                using (var repo = new Repository(listing.Item1.Path))
                {
                    var existingBranch = repo.Branches
                        .Where((b) => b.Tip.Sha.Equals(listing.Item2))
                        .OrderBy((b) => b.FriendlyName.Contains("GitHarmonize") ? 0 : 1)
                        .FirstOrDefault();
                    if (existingBranch != null)
                    {
                        this.WriteLine($"Checking out existing branch {existingBranch.FriendlyName}.");
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
                            this.WriteLine($"Creating {branchName}.");
                            var branch = repo.CreateBranch(branchName, listing.Item2);
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
                            this.WriteLine("Moving " + harmonizeBranch.FriendlyName + " to target commit.");
                            Commands.Checkout(repo, harmonizeBranch);
                            repo.Reset(ResetMode.Hard, listing.Item2);
                            return;
                        }
                    }
                }
                throw new NotImplementedException("Delete some branches.  You have over 100.");
            }
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
