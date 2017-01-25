import sys
import git
import commoncode
from git import Repo

print ("Test", sys.argv)
if sys.argv[1] == sys.argv[2]:
	sys.exit(0)
 
repo = Repo()
assert not repo.bare
for b in repo.branches:
	if b.commit.hexsha == "d5182dfb78782cb9b3d00ba6fabb07e6d619f161":
		print("Checking out", b)
		b.checkout()
	
#git checkout d5182dfb78782cb9b3d00ba6fabb07e6d619f161
	
sys.exit(1)