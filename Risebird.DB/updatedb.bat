@echo off
cls
set  SName="onesharenew.database.windows.net"
set  DbName="onesharenewPeerReview1"
set  UName="onesharedev"
set  Pwd="1$hare@ng@dev"

set /p choice=ARE YOU SURE TO EXECUTE SCRIPTS in %DbName% (y/n) ?

if '%choice%'=='y' goto begin
goto end

:begin
if exist _Deploy.txt del _Deploy.txt

@echo on

sqlcmd -S %SName% -U %UName% -P %Pwd% -d %DbName% -I -i OneShareDB_BuildScript.sql >> _Deploy.txt 2>&1



@notepad _Deploy.txt


:end 
