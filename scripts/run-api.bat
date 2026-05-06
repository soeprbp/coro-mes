@echo off
echo Starting CoroMES API...
cd /d "%~dp0..\src\CoroMES.Api"
dotnet run
pause