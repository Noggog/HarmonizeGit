import sys
from git import Repo
from commoncode import *

#If moving to the same commit, just exit
if sys.argv[1] == sys.argv[2]:
	sys.exit(0)
	
if not Lock("checkout"):
	print("Breaking out of harmonize checkout logic, as lock could not be achieved.")
	Unlock("checkout")
	sys.exit(1)
 
uncommittedRepos = GetReposWithUncommittedChanges()
if len(uncommittedRepos) != 0:
	print("Rolling back, because repos had uncommitted changes:")
	for repo in uncommittedRepos:
		print("   -", repo)
	CheckoutBranchAtCommit(sys.argv[1]) 
	sys.exit(1) 

print("No uncommitted changes in parent repos.")
Unlock("checkout")
sys.exit(0)