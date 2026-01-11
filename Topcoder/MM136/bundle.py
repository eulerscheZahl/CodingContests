#!/usr/bin/python3

name = 'HungryKnights'

import os
from os.path import isfile, join
from zipfile import ZipFile

usings = set()
code = []
for file in os.listdir('.'):
	if not file.endswith('.cs'): continue
	with open('./' + file) as f:
		lines = f.readlines()
	for line in lines:
		if line.startswith('using System'): usings.add(line)
		elif not line.startswith('#define') and not line.startswith('#undef'): code.append(line)
	code.append('\n')

with open(name+'.cs', 'w') as f:
	f.write(''.join(sorted(usings)))
	f.write('\n')
	f.write(''.join(code))

zipObj = ZipFile(name+'.zip', 'w')
zipObj.write(name+'.cs')
zipObj.close()

os.remove(name+".cs")
