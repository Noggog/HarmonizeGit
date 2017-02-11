import sys
from git import Repo
from commoncode import *

file = open(sys.argv[1], "a")
file.write("\n")
file.write("<HarmonizeGitMeta>\n")

config = ImportHarmonizeConfig(True)
for c in config.values():
    repo = Repo(c.Path)
    file.write("<Ref ")
    file.write("Nickname=\"" + c.Nickname + "\" ")
    file.write("Sha=\"" + repo.active_branch.commit.hexsha + "\" />")
    file.write("\n")

file.write("</HarmonizeGitMeta>")
file.close()
