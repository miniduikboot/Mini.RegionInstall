#!/usr/bin/env bash
set -euo pipefail

# The normal build
dotnet build -c Release
# Artifact can be found in Mini.RegionInstall/bin/Release/Mini.RegionInstall.dll

# building with reactor support
dotnet build -c Release \
    -p:DefineConstants=REACTOR \
    -p:OutputPath=bin-reactor/

cp ./Mini.RegionInstall/bin-reactor/Mini.RegionInstall.dll ./Mini.RegionInstall/bin-reactor/Mini.RegionInstall_Reactor.dll
# Artifact can be found in Mini.RegionInstall/bin-reactor/Mini.RegionInstall_Reactor.dll
