#!/bin/bash

dotnet publish Djinn/Djinn.csproj -c Release -o publish /p:PublishSingleFile=true /p:SelfContained=true /p:AssemblyName=djinn

mkdir -p ~/.local/bin

cp publish/djinn ~/.local/bin/

chmod +x ~/.local/bin/djinn
