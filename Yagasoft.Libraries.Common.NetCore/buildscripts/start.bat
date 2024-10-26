@echo OFF
cd ..
dotnet pack
powershell.exe -executionpolicy unrestricted .\buildscripts\publish-nuget-packages.ps1
