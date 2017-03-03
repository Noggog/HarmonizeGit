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
            await InsertChildEntries(
                parentRepoPath,
                usages);
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
                        commit,
                        harmonize.ConfigLoader.GetPathing(childRepoPath));
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
                            ChildPath = childRepoPath
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
                "create table ChildIdentity (Path varchar(1000))", conn).ExecuteNonQueryAsync();
            await new SQLiteCommand(
                "create table ParentSha (Sha varchar(1000))", conn).ExecuteNonQueryAsync();
            await new SQLiteCommand(
                "create table ChildUsage (Sha varchar(1000))", conn).ExecuteNonQueryAsync();
        }

        private async Task InsertChildEntries(
            string pathToRepo,
            IEnumerable<ChildUsage> usages)
        {
            using (var db = await GetConnection(pathToRepo))
            {
            }
        }
    }
}
