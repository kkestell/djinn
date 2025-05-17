#!/bin/bash

dotnet publish Music/Music.csproj -c Release -r linux-x64 -o publish /p:PublishSingleFile=true /p:SelfContained=true /p:AssemblyName=music

mkdir -p ~/.local/bin

cp publish/music ~/.local/bin/

chmod +x ~/.local/bin/music
