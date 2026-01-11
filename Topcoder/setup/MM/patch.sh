#!/bin/bash
cd tester
javac *.java
cd com/topcoder/marathon/
pwd
javac *.java
cd ../../..
zip -r ../tester.jar .
