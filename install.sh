#!/usr/bin/env bash

cd Djinn
dotnet publish -r linux-x64 -c Release /p:PublishSingleFile=true --self-contained true
cp ./bin/Release/net8.0/linux-x64/publish/djinn ~/.local/bin
chmod +x ~/.local/bin/djinn
rm -rf Djinn/bin/Release/net8.0/linux-x64/publish/
cd ..
