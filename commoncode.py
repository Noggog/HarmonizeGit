import os
import json
from git import Repo

class ConfigItem:
	Nickname = ""
	Path = ""
	
class GetResponse:
	Success = False
	StatusNote = ""
	def __init__(self, success, note):
		self.Success = success
		self.StatusNote = note

def ImportHarmonizeConfig():
	if not os.path.exists(".harmonize"):
		print("No harmonize config existed. Creating empty one.")
		file = open('.harmonize', 'w+')
		return ConfigItem()
	
	ret = []
	print("Loading harmonize config")
	with open('.harmonize') as data_file:
		data = json.load(data_file)
		repos = data["Parent-Repos"]
		for repo in repos:
			item = ConfigItem()
			item.Nickname = repo["Nickname"]
			item.Path = repo["Path"]
			ret.append(item)
	print("Found", len(ret), "config items.")
	return ret
	
def CheckoutBranchAtCommit(sha):
	repo = Repo()
	assert not repo.bare
	for b in repo.branches:
		if b.commit.hexsha == sha:
			print("Checking out", b)
			b.checkout()
			return
			
def CheckForUncommitedChanges(repoPath):
	repo = Repo(path = repoPath)
	if repo.bare:
		return GetResponse(True, "Repo was bare.")
	
	if repo.is_dirty(untracked_files=True):
		return GetResponse(True, "Repo had uncommitted changes.")
	else:
		return GetResponse(False, "")

def GetLockPath(lockName):
	return ".git/harmonize-lock-" + lockName
	
def Lock(lockName):
	lockPath = GetLockPath(lockName)
	if HasLock(lockName):
		return False
	file = open(lockPath, 'w+')
	return True
	
def HasLock(lockName):
	if os.path.isfile(GetLockPath(lockName)):
		return True
	return False
	
def Unlock(lockName):
	os.remove(GetLockPath(lockName))