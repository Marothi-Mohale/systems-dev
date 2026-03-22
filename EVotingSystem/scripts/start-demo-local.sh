#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."

export HOME=/tmp
export DOTNET_CLI_HOME=/tmp
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

cat <<'EOF'
Starting Election Platform in local demo mode.

- Firebase values from appsettings are blank by default, so the app will use demo-safe seeded candidates.
- Local demo reset is simple: stop the app and start it again.
- Identity storage is in-memory in this prototype, so demo users reset between restarts.
EOF

dotnet run --project EVotingSystem.csproj
