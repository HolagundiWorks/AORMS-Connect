@echo off
set MSB="%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
if not exist %MSB% set MSB="%ProgramFiles(x86)%\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
%MSB% "%~dp0src\AORMS-Connect.csproj" /t:Publish /p:Configuration=Release /p:Platform=x64 /p:WindowsPackageType=MSIX /p:GenerateAppxPackageOnBuild=true /p:AppxPackageSigningEnabled=false /restore /v:m
echo.
echo Packages land under artifacts\msix\ (unsigned until D6).
