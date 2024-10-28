@echo OFF
cd ../bin/Release
del *.nupkg
cd ../..
dotnet pack
xcopy .\bin\Release\*.nupkg c:\Nuget.Local
powershell.exe -executionpolicy unrestricted .\buildscripts\publish-nuget-packages.ps1
