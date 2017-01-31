import sys
from git import Repo
from commoncode import *
 
uncommittedRepos = GetReposWithUncommittedChanges()
if len(uncommittedRepos) != 0:
	print("Blocked commit, because repos had uncommitted changes:")
	for repo in uncommittedRepos:
		print("   -", repo)
	sys.exit(1) 

print("No uncommitted changes in parent repos.")
sys.exit(0)