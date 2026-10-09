@echo off
cd /d "%~dp0"
dotnet publish src\MouseFood.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o Dist
