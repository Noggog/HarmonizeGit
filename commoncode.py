import os
import json
from git import Repo

class ConfigItem:
	Nickname = ""
	Path = ""

def ImportHarmonizeConfig():
	if not os.path.exists(".harmonize"):
		file = open('.harmonize', 'w+')
		return ConfigItem()
	
	ret = []
	
	with open('.harmonize') as data_file:
		data = json.load(data_file)
		repos = data["Parent-Repos"]
		for repo in repos:
			item = ConfigItem()
			item.Nickname = repo["Nickname"]
			item.Path = repo["Path"]
			ret.append(item)
	return ret
	
def CheckoutBranchAtCommit(sha):
	repo = Repo()
	assert not repo.bare
	for b in repo.branches:
		if b.commit.hexsha == sha:
			print("Checking out", b)
			b.checkout()
			return