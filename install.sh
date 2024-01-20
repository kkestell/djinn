#!/usr/bin/env bash

cd Djinn
dotnet publish -r linux-x64 -c Release /p:PublishSingleFile=true --self-contained true -o publish
cp publish/Djinn ~/.local/bin/djinn
chmod +x ~/.local/bin/djinn
rm -rf publish
cd ..
