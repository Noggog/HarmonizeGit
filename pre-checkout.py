import sys
from git import Repo
from gitdb.exc import BadName
from commoncode import *

# If moving to the same commit, just exit
if sys.argv[1] == sys.argv[2]:
    sys.exit(0)

# Check parent repos for uncommitted changes
uncommittedRepos = GetReposWithUncommittedChanges()
if len(uncommittedRepos) != 0:
    print("Cancelling because repos had uncommitted changes:")
    for repo in uncommittedRepos:
        print("   -", repo)
    sys.exit(1)

print("No uncommitted changes in parent repos.")

# Load target commit message for parent repo sha codes
print("Getting target shas for parent repos.")
parentShasResp = GetShasForParentRepoCommits()
if not parentShasResp.Success:
    print("Error loading parent shas:", parentShasResp.StatusNote)
    sys.exit(1)

# Loop over parent repos and check out target commits
print("Checking out commits in parent repos.")
for config in parentShasResp.Item:
    repo = Repo(config.Path)
    print("Processing", config.Nickname, ".  Trying to check out an existing branch.")
    if not CheckoutBranchAtCommit(repo, config.Sha):
        # Didn't have a branch already at our target commit.  Need to make one
        # Find an available GitHarmonize branch
        for i in range(0, 25):
            # Create target branch name
            branchName = "GitHarmonize"
            if i != 0:
                branchName += str(i)

            # Try to get it
            cur = GetBranch(repo, branchName)
            if cur is None:
                print("Creating new branch", branchName)
                # If branch doesn't exist, we can make it and be done
                repo.create_head(branchName, config.Sha)
                if not CheckoutBranchAtCommit(repo, config.Sha):
                    print("Failed to create GitHarmonize branch.")
                    sys.exit(1)
                break

            # Check if it is the last branch pointing to a commit.
            # We don't want to steal that one
            print("Checking", branchName, "branch to see if it is the only branch pointing to its commit.")
            if GetIsLoneTip(repo, cur.commit.hexsha, branchName):
                print(branchName, "was unsafe to move.")
                continue

            # Just move the branch to our target commit and checkout.
            print("Moving", branchName, "to the target commit.")

            cur.commit = repo.commit(config.Sha)
            cur.checkout()

sys.exit(1)
