#!/usr/bin/env bash
set -euo pipefail

mkdir -p output

# The normal build
dotnet build -c Release \
    -p:OutputPath=../output/bin-noreactor/
# Artifact can be found in Mini.RegionInstall/bin/Release/Mini.RegionInstall.dll

# building with reactor support
dotnet build -c Release \
    -p:DefineConstants=REACTOR \
    -p:OutputPath=../output/bin/

# build source code zip
git archive --format zip --output output/source.zip HEAD

cat output/bin-noreactor/Mini.RegionInstall.dll output/source.zip >output/Mini.RegionInstall-NoReactor.dll
cat output/bin/Mini.RegionInstall.dll output/source.zip >output/Mini.RegionInstall.dll
#cp ./Mini.RegionInstall/bin-reactor/Mini.RegionInstall.dll ./Mini.RegionInstall/bin-reactor/Mini.RegionInstall_Reactor.dll
# Artifact can be found in Mini.RegionInstall/bin-reactor/Mini.RegionInstall_Reactor.dll
