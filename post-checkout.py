import sys
from git import Repo
from commoncode import *

# If moving to the same commit, just exit
if sys.argv[1] == sys.argv[2]:
    sys.exit(0)

# Check lock and break early
if not Lock("checkout"):
    print("Breaking out of harmonize checkout logic, as lock could not be achieved.")
    Unlock("checkout")
    sys.exit(1)

# Check parent repos for uncommitted changes
uncommittedRepos = GetReposWithUncommittedChanges()
if len(uncommittedRepos) != 0:
    print("Rolling back, because repos had uncommitted changes:")
    for repo in uncommittedRepos:
        print("   -", repo)
    CheckoutBranchAtCommit(Repo(), sys.argv[1])
    sys.exit(1)

print("No uncommitted changes in parent repos.")

# Load target commit message for parent repo sha codes
parentShasResp = GetShasForParentRepoCommits()
if not parentShasResp.Success:
    print("Error loading parent shas:", parentShasResp.StatusNote)
    sys.exit(1)

for config in parentShasResp.Item:
    print("HMM", config.Path)
    repo = Repo(config.Path)
    if not CheckoutBranchAtCommit(repo, config.Sha):
        repo.create_head("GitHarmonize", config.Sha)
        if not CheckoutBranchAtCommit(repo, config.Sha):
            print("Failed to create GitHarmonize branch.")
            sys.exit(1)

Unlock("checkout")
sys.exit(1)
