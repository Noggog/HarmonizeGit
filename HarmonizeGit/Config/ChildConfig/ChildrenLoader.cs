﻿using LibGit2Sharp;
using Noggog;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HarmonizeGit
{
    public class ChildrenLoader
    {
        private HarmonizeGitBase harmonize;
        public const string PARENT_TABLE = "ParentRef";
        public const string CHILD_IDENTITY_TABLE = "ChildIdentity";
        public const string USAGE_TABLE = "ChildUsage";
        public const string SHA = "Sha";
        public const string PATH = "Path";
        public const string ID = "ID";
        public const string PARENT_ID = "ParentID";
        public const string IDENTITY_ID = "IdentityID";

        public ChildrenLoader(HarmonizeGitBase harmonize)
        {
            this.harmonize = harmonize;
        }

        public async Task InitializeIntoParents()
        {
            if (!Settings.Instance.TrackChildRepos) return;
            await Task.WhenAll(
                harmonize.Config.ParentRepos.Select(
                    async (parentRepo) =>
                    {
                        FileInfo dbPath = new FileInfo(GetDBPath(parentRepo.Path));
                        if (dbPath.Exists) return;
                        this.harmonize.Logger.WriteLine($"Initilizing into parent: {parentRepo.Path}");
                        using (LockManager.GetLock(LockType.Child, parentRepo.Path))
                        {
                            if (dbPath.Exists) return;
                            await CheckAndSeed(
                                parentRepo.Path,
                                harmonize.TargetPath);
                        }
                        this.harmonize.Logger.WriteLine($"Initilized into parent: {parentRepo.Path}");
                    }));
        }

        public async Task CheckAndSeed(
            string parentRepoPath,
            string childRepoPath)
        {
            var parentRepoFile = new FileInfo(parentRepoPath);
            parentRepoPath = parentRepoFile.FullName;
            this.harmonize.Logger.WriteLine("Getting seed usages.");
            var usages = await GetUsages(
                parentRepoPath,
                childRepoPath);
            this.harmonize.Logger.WriteLine("Got seed usages.");
            await InsertChildEntries(usages);
        }

        private async Task<List<ChildUsage>> GetUsages(
            string parentRepoPath,
            string childRepoPath)
        {
            var usages = new List<ChildUsage>();
            var childRepo = this.harmonize.RepoLoader.GetRepo(childRepoPath);
            foreach (var commit in childRepo.Commits)
            {
                var parentUsage = GetUsages(
                    childRepo,
                    commit)
                    .Where((usage) => usage.ParentRepoPath.Equals(parentRepoPath))
                    .FirstOrDefault();
                if (parentUsage != null)
                {
                    usages.Add(parentUsage);
                }
            }
            return usages;
        }

        public IEnumerable<ChildUsage> GetUsages(
            Repository repo,
            Commit commit)
        {
            var config = HarmonizeConfig.Factory(
                harmonize.ConfigLoader,
                repo,
                commit);
            if (config == null) yield break;
            foreach (var parentListing in config.ParentRepos)
            {
                yield return new ChildUsage()
                {
                    Sha = commit.Sha,
                    ParentSha = parentListing.Sha,
                    ChildRepoPath = repo.Info.WorkingDirectory,
                    ParentRepoPath = parentListing.Path
                };
            }
        }

        public IEnumerable<ChildUsage> GetUsages(
            Repository repo,
            IEnumerable<Commit> commits)
        {
            return commits.SelectMany(
                (commit) => GetUsages(repo, commit));
        }

        private string GetDBPath(string pathToRepo)
        {
            return pathToRepo + "/" + Constants.HarmonizeChildrenPath + ".db";
        }

        private async Task<SQLiteConnection> GetConnection(string pathToRepo)
        {
            FileInfo dbPath = new FileInfo(GetDBPath(pathToRepo));
            bool create = !dbPath.Exists;
            var conn = new SQLiteConnection($"Data Source={GetDBPath(pathToRepo)}");
            await conn.OpenAsync();
            if (create)
            {
                await ConstructDB(conn);
            }
            return conn;
        }

        private async Task ConstructDB(SQLiteConnection conn)
        {
            using (var cmd = new SQLiteCommand(
                $@"create table {CHILD_IDENTITY_TABLE} (
                    {ID} integer primary key AUTOINCREMENT NOT NULL,
                    {PATH} varchar(1000) not null unique)", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }
            using (var cmd = new SQLiteCommand(
                $@"create table {PARENT_TABLE} (
                    {ID} integer primary key AUTOINCREMENT NOT NULL,
                    {SHA} varchar(1000) not null unique)", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }
            using (var cmd = new SQLiteCommand(
                $@"create table {USAGE_TABLE} (
                    {ID} integer primary key AUTOINCREMENT NOT NULL,
                    {PARENT_ID} integer,
                    {IDENTITY_ID} integer,
                    {SHA} char(40) not null unique,
                    FOREIGN KEY({PARENT_ID}) REFERENCES {PARENT_TABLE}({ID}),
                    FOREIGN KEY({IDENTITY_ID}) REFERENCES {CHILD_IDENTITY_TABLE}({ID}))", conn))
            {
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public Task InsertChildEntry(ChildUsage usage)
        {
            return InsertChildEntries(new ChildUsage[] { usage });
        }

        public Task InsertChildEntries(IEnumerable<ChildUsage> usages)
        {
            return Task.WhenAll(
                usages.GroupBy((u) => u.ParentRepoPath)
                .Select((group) => InsertChildEntries(group.Key, group)));
        }

        private async Task InsertChildEntries(
            string pathToRepo,
            IEnumerable<ChildUsage> usages)
        {
            this.harmonize.Logger.WriteLine($"Inserting child usages into {pathToRepo}");
            using (var conn = await GetConnection(pathToRepo))
            {
                using (var transaction = conn.BeginTransaction())
                {
                    using (var cmd = new SQLiteCommand(conn))
                    {
                        // Child identity
                        foreach (var childRepoPath in usages.Select((u) => u.ChildRepoPath).Distinct())
                        {
                            cmd.CommandText = $@"INSERT OR IGNORE INTO {CHILD_IDENTITY_TABLE} ({PATH}) 
                                        SELECT '{childRepoPath}'
                                        WHERE NOT EXISTS(SELECT 1 FROM {CHILD_IDENTITY_TABLE} WHERE {PATH} = '{childRepoPath}');";
                            await cmd.ExecuteNonQueryAsync();
                        }

                        // Parent sha table
                        foreach (var parentRef in usages.Select((u) => u.ParentSha).Distinct())
                        {
                            cmd.CommandText = $@"INSERT OR IGNORE INTO {PARENT_TABLE} ({SHA}) 
                                        SELECT '{parentRef}'
                                        WHERE NOT EXISTS(SELECT 1 FROM {PARENT_TABLE} WHERE {SHA} = '{parentRef}')";
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    transaction.Commit();
                }

                using (var transaction = conn.BeginTransaction())
                {
                    using (var cmd = new SQLiteCommand(conn))
                    {
                        foreach (var usage in usages)
                        {
                            if (usage.Sha.Length != 40)
                            {
                                throw new ArgumentException("Sha length was not 40 characters: " + usage.Sha);
                            }
                            this.harmonize.Logger.WriteLine($"   {usage.Sha} using {usage.ParentSha}");
                            // Usages
                            cmd.CommandText = $@"INSERT OR IGNORE INTO {USAGE_TABLE} ({PARENT_ID}, {IDENTITY_ID}, {SHA}) 
                                        VALUES (
                                            (SELECT {ID} FROM {PARENT_TABLE} WHERE {SHA} = '{usage.ParentSha}'),
                                            (SELECT {ID} FROM {CHILD_IDENTITY_TABLE} WHERE {PATH} = '{usage.ChildRepoPath}'),
                                            '{usage.Sha}');";
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    transaction.Commit();
                }
            }
        }

        public Task RemoveChildEntries(IEnumerable<ChildUsage> usages)
        {
            return Task.WhenAll(
                usages.GroupBy((u) => u.ParentRepoPath)
                .Select((group) => RemoveChildEntries(group.Key, group)));
        }

        private async Task RemoveChildEntries(
            string pathToRepo,
            IEnumerable<ChildUsage> usages)
        {
            this.harmonize.Logger.WriteLine($"Removing child usages from {pathToRepo}");
            using (var conn = await GetConnection(pathToRepo))
            {
                using (var transaction = conn.BeginTransaction())
                {
                    using (var cmd = new SQLiteCommand(conn))
                    {
                        foreach (var usage in usages)
                        {
                            this.harmonize.Logger.WriteLine($"   {usage.Sha} used {usage.ParentSha}");
                            // Usages
                            cmd.CommandText = $@"DELETE FROM {USAGE_TABLE}
                                        WHERE {SHA} = '{usage.Sha}' 
                                        AND {PARENT_ID} IN (
                                            SELECT pt.ID FROM {USAGE_TABLE} ut
                                            INNER JOIN {PARENT_TABLE} pt 
                                            ON pt.{ID} = ut.{PARENT_ID})
                                        AND {IDENTITY_ID} IN (
                                            SELECT it.ID FROM {USAGE_TABLE} ut
                                            INNER JOIN {CHILD_IDENTITY_TABLE} it 
                                            ON it.{ID} = ut.{IDENTITY_ID});";
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    transaction.Commit();
                }
            }
        }

        private IEnumerable<ChildUsage> GetCurrentConfigUsagesFromConfig()
        {
            string currentSha;
            var repo = this.harmonize.Repo;
            currentSha = repo.Head.Tip.Sha;
            return GetUsagesFromConfig(
                this.harmonize.Config,
                currentSha);
        }

        public IEnumerable<ChildUsage> GetUsagesFromConfig(
            HarmonizeConfig config,
            string configSha)
        {
            foreach (var parentListing in this.harmonize.Config.ParentRepos)
            {
                yield return new ChildUsage()
                {
                    ParentRepoPath = parentListing.Path,
                    ChildRepoPath = this.harmonize.TargetPath,
                    ParentSha = parentListing.Sha,
                    Sha = configSha
                };
            }
        }

        public async Task<GetResponse<ChildUsage>> LookupChildUsage(string childSha)
        {
            using (var conn = await GetConnection(this.harmonize.TargetPath))
            {
                using (var cmd = new SQLiteCommand(conn))
                {
                    cmd.CommandText =
$@"SELECT 
	ParentRef.Sha,
	ChildIdentity.Path
FROM ChildUsage
INNER JOIN ParentRef
ON ParentRef.ID = ChildUsage.ParentID
INNER JOIN ChildIdentity
ON ChildIdentity.ID = ChildUsage.IdentityID 
where ChildUsage.Sha = '{childSha}'";
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            return GetResponse<ChildUsage>.Succeed(
                                new ChildUsage()
                                {
                                    Sha = childSha,
                                    ParentSha = (string)reader[0],
                                    ChildRepoPath = (string)reader[1],
                                    ParentRepoPath = this.harmonize.TargetPath
                                });
                        }
                    }
                    return new GetResponse<ChildUsage>();
                }
            }
        }

        public async Task InsertCurrentConfig()
        {
            await InsertChildEntries(GetCurrentConfigUsagesFromConfig());
        }

        public async Task RemoveCurrentConfigFromParents()
        {
            await RemoveChildEntries(GetCurrentConfigUsagesFromConfig());
        }

        public async Task<ChildUsages> GetChildUsages(
            IEnumerable<string> commits)
        {
            HashSet<string> usedCommits = new HashSet<string>();
            HashSet<string> childRepos = new HashSet<string>();
            List<ChildUsage>[] results;
            using (var conn = await GetConnection(this.harmonize.TargetPath))
            {
                results = await Task.WhenAll(
                    commits.Select(
                        async (commit) =>
                        {
                            using (var cmd = new SQLiteCommand(conn))
                            {
                                cmd.CommandText =
$@"SELECT 
	ParentRef.Sha,
	ChildIdentity.Path
FROM ChildUsage
INNER JOIN ParentRef
ON ParentRef.ID = ChildUsage.ParentID
INNER JOIN ChildIdentity
ON ChildIdentity.ID = ChildUsage.IdentityID 
where ParentRef.Sha = '{commit}'";
                                List<ChildUsage> ret = new List<ChildUsage>();
                                using (var reader = await cmd.ExecuteReaderAsync())
                                {
                                    while (reader.Read())
                                    {
                                        ret.Add(new ChildUsage()
                                        {
                                            ParentSha = (string)reader[0],
                                            ChildRepoPath = (string)reader[1]
                                        });
                                    }
                                }
                                return ret;
                            }
                        }));
            }

            foreach (var pair in results.SelectMany((x) => x))
            {
                usedCommits.Add(pair.ParentSha);
                childRepos.Add(pair.ChildRepoPath);
            }

            return new ChildUsages()
            {
                UsedCommits = usedCommits,
                ChildRepos = childRepos
            };
        }
    }
}
