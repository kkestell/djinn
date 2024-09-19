set shell := ["bash", "-c"]

# Default recipe to run when just is called without arguments
default:
    @just --list

# Check if required commands are available
check:
    @command -v eyeD3 >/dev/null 2>&1 || { echo >&2 "eyeD3 is not installed. Aborting."; exit 1; }
    @command -v metaflac >/dev/null 2>&1 || { echo >&2 "metaflac is not installed. Aborting."; exit 1; }

# Build the project
build: check
    cd Djinn && dotnet build

# Install the application
install: build
    cd Djinn && \
    dotnet publish -r linux-x64 -c Release /p:PublishSingleFile=true --self-contained true -o publish && \
    mkdir -p ~/.local/bin && \
    install -m 755 publish/djinn ~/.local/bin/

# Uninstall the application
uninstall:
    rm -f ~/.local/bin/djinn

# Clean build artifacts
clean:
    cd Djinn && \
    rm -rf bin obj publish
