using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
        public EventWaitHandle dbSyncer = new EventWaitHandle(true, EventResetMode.AutoReset, "GIT_HARMONIZE_CHILDDB_SYNCER");

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
                        dbSyncer.WaitOne();
                        try
                        {
                            if (dbPath.Exists) return;
                            await CheckAndSeed(
                                parentRepo.Path,
                                harmonize.TargetPath);
                        }
                        finally
                        {
                            dbSyncer.Set();
                        }
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
            return GetConfigUsages(
                this.harmonize.Config,
                currentSha);
        }

        public IEnumerable<ChildUsage> GetConfigUsages(
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

        public async Task InsertCurrentConfig()
        {
            await InsertChildEntries(GetCurrentConfigUsages());
        }

        public async Task RemoveCurrentConfigFromParents()
        {
            await RemoveChildEntries(GetCurrentConfigUsages());
        }

        public async Task<Tuple<ICollection<string>, ICollection<string>>> GetChildUsages(
            IEnumerable<string> commits,
            int numCommitsToReturn)
        {
            HashSet<string> usedCommits = new HashSet<string>();
            HashSet<string> childRepos = new HashSet<string>();
            List<Tuple<string, string>>[] results;
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
                                List<Tuple<string, string>> ret = new List<Tuple<string, string>>();
                                using (var reader = await cmd.ExecuteReaderAsync())
                                {
                                    while (reader.Read())
                                    {
                                        ret.Add(
                                            new Tuple<string, string>(
                                                (string)reader[0],
                                                (string)reader[1]));
                                    }
                                }
                                return ret;
                            }
                        }));
            }

            foreach (var pair in results.SelectMany((x) => x))
            {
                usedCommits.Add(pair.Item1);
                childRepos.Add(pair.Item2);
            }

            return new Tuple<ICollection<string>, ICollection<string>>(
                usedCommits,
                childRepos);
        }
    }
}
