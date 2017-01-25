import os

class ConfigItem:
	Nickname = ""
	Path = ""

def ImportHarmonizeConfig():
	if not os.path.exists(".harmonize"):
		file = open('.harmonize', 'w+')
		return ConfigItem()
	
#	with open('inputfile.txt') as inputfile:
#		for line in inputfile: