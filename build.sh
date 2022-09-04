#!/usr/bin/env bash
set -euo pipefail

mkdir -p output

# The normal build
dotnet build -c Release \
	-p:OutputPath=../output/bin/
# Artifact can be found in Mini.RegionInstall/bin/Release/Mini.RegionInstall.dll

# build source code zip
git archive --format zip --output output/source.zip HEAD

cat output/bin/Mini.RegionInstall.dll output/source.zip >output/Mini.RegionInstall.dll
#cp ./Mini.RegionInstall/bin-reactor/Mini.RegionInstall.dll ./Mini.RegionInstall/bin-reactor/Mini.RegionInstall_Reactor.dll
# Artifact can be found in Mini.RegionInstall/bin-reactor/Mini.RegionInstall_Reactor.dll
