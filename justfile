check:
  @command -v eyeD3 >/dev/null 2>&1 || { echo "eyeD3 is not installed. Aborting."; exit 1; }
  @command -v metaflac >/dev/null 2>&1 || { echo "metaflac is not installed. Aborting."; exit 1; }

build: check
  cd Djinn && dotnet build

install: build
  cd Djinn && \
  dotnet publish -r linux-x64 -c Release /p:PublishSingleFile=true --self-contained true -o publish && \
  mkdir -p ~/.local/bin && \
  cp publish/djinn ~/.local/bin/

uninstall:
  rm -f ~/.local/bin/djinn

clean:
  cd Djinn && \
  rm -rf bin obj publish
