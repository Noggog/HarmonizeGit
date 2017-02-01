import os
import json
from git import Repo
import xml.etree.ElementTree


class ConfigItem:
    Nickname = ""
    Path = ""
    Sha = ""

    def __str__(self):
        return self.Nickname + " (" + self.Path + ")"


class GetResponse:
    Success = False
    Item = ""
    StatusNote = ""

    def __init__(self, success, item=None, note=""):
        self.Success = success
        self.Item = item
        self.StatusNote = note


def ImportHarmonizeConfig():
    if not os.path.exists(".harmonize"):
        print("No harmonize config existed. Creating empty one.")
        file = open('.harmonize', 'w+')
        return ConfigItem()

    ret = {}
    print("Loading harmonize config")
    with open('.harmonize') as data_file:
        data = json.load(data_file)
        repos = data["Parent-Repos"]
        for repo in repos:
            item = ConfigItem()
            item.Nickname = repo["Nickname"]
            item.Path = repo["Path"]
            ret[item.Nickname] = item
    print("Found", len(ret), "config items.")
    return ret


def CheckoutBranchAtCommit(repo, sha):
    assert not repo.bare
    for b in repo.branches:
        if b.commit.hexsha == sha:
            print("Checking out", b)
            b.checkout()
            return True
    return False


def GetReposWithUncommittedChanges():
    config = ImportHarmonizeConfig()

    repos = []
    for parentRepo in config.values():
        print("Checking", parentRepo.Nickname, "for changes. (", parentRepo.Path, ")")
        check = CheckForUncommitedChanges(parentRepo.Path)
        if check.Success:
            repos.append(parentRepo)

    return repos


def CheckForUncommitedChanges():
    repos = GetReposWithUncommittedChanges()
    return len(repos) != 0

def GetBranch(repo: Repo, branchName: str):
    for b in repo.branches:
        if b.name == branchName:
            return b
    return None


def CheckForUncommitedChanges(repoPath):
    repo = Repo(path=repoPath)
    if repo.bare:
        return GetResponse(True, note="Repo was bare.")

    if repo.is_dirty(untracked_files=True):
        return GetResponse(True, note="Repo had uncommitted changes.")
    else:
        return GetResponse(False, note="")

def GetIsLoneTip(repo, sha, branchName):
    ret = repo.git.execute("git branch --contains " + sha + " | grep -v \"" + branchName + "\"", shell=True)
    return ret.isspace() or not ret

def GetShasForParentRepoCommits():
    config = ImportHarmonizeConfig()
    repo = Repo()
    ret = []
    msg = repo.head.commit.message
    index = msg.rfind("<HarmonizeGitMeta>")
    if index == -1:
        return GetResponse(False, note="No HarmonizeGit metadata found in commit message.")

    dupIndex = msg.find("<HarmonizeGitMeta>")
    if index != dupIndex:
        return GetResponse(False, note="Multiple HarmonizeGit metadata found in commit message.")

    endNode = "</HarmonizeGitMeta>"
    endIndex = msg.rfind(endNode)
    if endIndex == -1:
        return GetResponse(False, note="No End metadata xml node.")

    msg = msg[index:endIndex + len(endNode)]
    e = xml.etree.ElementTree.fromstring(msg)

    for ref in e.findall("Ref"):
        item = ConfigItem()
        item.Nickname = ref.get("Nickname")
        item.Sha = ref.get("Sha")
        item.Path = config[item.Nickname].Path
        ret.append(item)

    return GetResponse(True, ret, "")


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
