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
 
config = ImportHarmonizeConfig()

for parentRepo in config:
	print("Checking", parentRepo.Nickname, "for changes. (", parentRepo.Path, ")")
	check = CheckForUncommitedChanges(parentRepo.Path)
	if check.Success:
		print(parentRepo.Nickname, "had uncommitted changes.  Reversing checkout.")
		CheckoutBranchAtCommit(sys.argv[1])
		sys.exit(1)

print(parentRepo.Nickname, "had no uncommitted changes.")
Unlock("checkout")
sys.exit(0)