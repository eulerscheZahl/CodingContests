#!/bin/bash
time java -jar tester.jar -exec "dotnet Solution/bin/Release/net6.0/Solution.dll" -delay 1 -seed 1,100 -th 8 -novis > scores100/$1.txt
