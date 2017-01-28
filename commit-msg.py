import sys
from git import Repo
from commoncode import *

file = open(sys.argv[1], "a")
file.write("\n")
file.write("HarmonizeGit\n")

config = ImportHarmonizeConfig()
for c in config:
	repo = Repo(c.Path)
	file.write(c.Nickname + ":" + repo.active_branch.commit.hexsha + "\n")
	
file.close()