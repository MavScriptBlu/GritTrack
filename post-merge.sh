#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."
dotnet restore GritTrack.Api/GritTrack.Api.csproj
dotnet build GritTrack.Api/GritTrack.Api.csproj --no-restore
