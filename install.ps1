$ErrorActionPreference = 'Stop'

dotnet publish Djinn/Djinn.csproj -c Release -r win-x64 -o publish /p:PublishSingleFile=true /p:SelfContained=true /p:ApplicationIcon=..\assets\icon.ico /p:AssemblyName=Djinn

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" install.iss
