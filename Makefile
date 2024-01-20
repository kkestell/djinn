.PHONY: debug release publish install clean

SOLUTION := Djinn.sln
PROJECT := ./Djinn/Djinn.csproj
PUBLISH_DIR := ./publish

PREFIX ?= ~/.local
INSTALL_DIR := $(PREFIX)/bin

debug:
	dotnet build $(SOLUTION) -c Debug

release:
	dotnet build $(SOLUTION) -c Release

publish:
	dotnet publish $(PROJECT) -c Release -o $(PUBLISH_DIR)

install: publish
	@echo "Installing application to $(INSTALL_DIR)"
	mkdir -p $(INSTALL_DIR)
	cp -r $(PUBLISH_DIR)/. $(INSTALL_DIR)

clean:
	dotnet clean $(SOLUTION)
	rm -rf $(PUBLISH_DIR)
