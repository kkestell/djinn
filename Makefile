.PHONY: all build install uninstall clean check help

PREFIX := ~/.local
BINDIR := $(PREFIX)/bin

all: build

check:
	@command -v eyeD3 >/dev/null 2>&1 || { echo >&2 "eyeD3 is not installed. Aborting."; exit 1; }
	@command -v metaflac >/dev/null 2>&1 || { echo >&2 "pandoc is not installed. Aborting."; exit 1; }

build: check
	cd Djinn && dotnet build

install: build
	cd Djinn && \
	dotnet publish -r linux-x64 -c Release /p:PublishSingleFile=true --self-contained true -o publish && \
	mkdir -p $(BINDIR) && \
	cp publish/djinn $(BINDIR)/

uninstall:
	rm -f $(BINDIR)/djinn

clean:
	cd Djinn && \
	rm -rf bin obj publish

help:
	@echo "Usage: make [target]"
	@echo "Available targets:"
	@echo "  all       - Build the project (default target)"
	@echo "  build     - Build the project"
	@echo "  install   - Install the project to BINDIR under PREFIX"
	@echo "  uninstall - Remove installed files from BINDIR"
	@echo "  clean     - Clean up build artifacts"
	@echo "  check     - Check for required commands"
	@echo "  help      - Display this help"
	@echo "Customization Variables:"
	@echo "  PREFIX    - Set the installation root directory (default: ~/.local)"
	@echo "              Example: make install PREFIX=/usr/local"
	@echo "  BINDIR    - Set the binary installation directory (default: $(PREFIX)/bin)"
	@echo "              Example: make install BINDIR=/usr/bin"
	@echo "Note: BINDIR is relative to PREFIX unless an absolute path is given."
