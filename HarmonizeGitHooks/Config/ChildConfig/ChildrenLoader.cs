using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HarmonizeGitHooks
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
            if (!Properties.Settings.Default.TrackChildRepos) return;
            await Task.WhenAll(
                harmonize.Config.ParentRepos.Select(
                    async (parentRepo) =>
                    {
                        FileInfo dbPath = new FileInfo(GetDBPath(parentRepo.Path));
                        if (dbPath.Exists) return;
                        await CheckAndSeed(
                            parentRepo.Path,
                            harmonize.TargetPath);
                    }));
        }

        public async Task CheckAndSeed(
            string parentRepoPath,
            string childRepoPath)
        {
            var parentRepoFile = new FileInfo(parentRepoPath);
            parentRepoPath = parentRepoFile.FullName;
            var usages = await GetUsages(
                parentRepoPath,
                childRepoPath);
            await InsertChildEntries(usages);
        }

        private async Task<List<ChildUsage>> GetUsages(
            string parentRepoPath,
            string childRepoPath)
        {
            var usages = new List<ChildUsage>();
            using (var childRepo = new Repository(childRepoPath))
            {
                foreach (var commit in childRepo.Commits)
                {
                    var config = HarmonizeConfig.Factory(
                        harmonize,
                        childRepoPath,
                        commit);
                    if (config == null) continue;
                    var parentListing = config.ParentRepos.Where(
                        (listing) =>
                        {
                            FileInfo listingPath = new FileInfo(listing.Path);
                            return object.Equals(listingPath.FullName, parentRepoPath);
                        }).FirstOrDefault();
                    if (parentListing == null) continue;
                    usages.Add(
                        new ChildUsage()
                        {
                            Sha = commit.Sha,
                            ParentSha = parentListing.Sha,
                            ChildRepoPath = childRepoPath,
                            ParentRepoPath = parentRepoPath
                        });
                }
            }
            return usages;
        }

        private string GetDBPath(string pathToRepo)
        {
            return pathToRepo + "/" + HarmonizeGitBase.HarmonizeChildrenPath + ".db";
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
            await new SQLiteCommand(
                $@"create table {CHILD_IDENTITY_TABLE} (
                    {ID} integer primary key AUTOINCREMENT NOT NULL,
                    {PATH} varchar(1000) not null unique)", conn).ExecuteNonQueryAsync();
            await new SQLiteCommand(
                $@"create table {PARENT_TABLE} (
                    {ID} integer primary key AUTOINCREMENT NOT NULL,
                    {SHA} varchar(1000) not null unique)", conn).ExecuteNonQueryAsync();
            await new SQLiteCommand(
                $@"create table {USAGE_TABLE} (
                    {ID} integer primary key AUTOINCREMENT NOT NULL,
                    {PARENT_ID} integer,
                    {IDENTITY_ID} integer,
                    {SHA} char(40) not null unique,
                    FOREIGN KEY({PARENT_ID}) REFERENCES {PARENT_TABLE}({ID}),
                    FOREIGN KEY({IDENTITY_ID}) REFERENCES {CHILD_IDENTITY_TABLE}({ID}))", conn).ExecuteNonQueryAsync();
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
                                        VALUES ('{childRepoPath}');";
                            await cmd.ExecuteNonQueryAsync();
                        }

                        // Parent sha table
                        foreach (var parentRef in usages.Select((u) => u.ParentSha).Distinct())
                        {
                            cmd.CommandText = $@"INSERT OR IGNORE INTO {PARENT_TABLE} ({SHA}) 
                                        VALUES ('{parentRef}');";
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
            using (var conn = await GetConnection(pathToRepo))
            {
                using (var transaction = conn.BeginTransaction())
                {
                    using (var cmd = new SQLiteCommand(conn))
                    {
                        foreach (var usage in usages)
                        {
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

        private IEnumerable<ChildUsage> GetCurrentConfigUsages()
        {
            string currentSha;
            using (var repo = new Repository(this.harmonize.TargetPath))
            {
                currentSha = repo.Head.Tip.Sha;
            }
            
            foreach (var parentListing in this.harmonize.Config.ParentRepos)
            {
                yield return new ChildUsage()
                {
                    ParentRepoPath = parentListing.Path,
                    ChildRepoPath = this.harmonize.TargetPath,
                    ParentSha = parentListing.Sha,
                    Sha = currentSha
                };
            }
        }

        public async Task InsertCurrentConfig()
        {
            await InsertChildEntries(GetCurrentConfigUsages());
        }

        public async Task RemoveCurrentConfig()
        {
            await RemoveChildEntries(GetCurrentConfigUsages());
        }
    }
}
