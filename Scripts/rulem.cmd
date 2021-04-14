@echo off

IF [%1]==[] ECHO Please set the file name && GOTO END

python rulem.py %1