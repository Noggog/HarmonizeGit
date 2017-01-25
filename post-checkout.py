import sys
import git
from commoncode import ImportHarmonizeConfig, ConfigItem
from git import Repo

print ("Test", sys.argv)
if sys.argv[1] == sys.argv[2]:
	sys.exit(0)
 
config = ImportHarmonizeConfig()
 
repo = Repo()
assert not repo.bare
for b in repo.branches:
	if b.commit.hexsha == "dfb3ee04c02d797368d8b140a498ef2dacadc263":
		print("Checking out", b)
		b.checkout() 
	
sys.exit(1)