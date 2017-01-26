import sys
from git import Repo
from commoncode import ImportHarmonizeConfig, ConfigItem, CheckoutBranchAtCommit

if sys.argv[1] == sys.argv[2]:
	sys.exit(0)
 
config = ImportHarmonizeConfig()

CheckoutBranchAtCommit("dfb3ee04c02d797368d8b140a498ef2dacadc263")
	
sys.exit(1)