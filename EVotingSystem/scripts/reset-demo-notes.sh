#!/usr/bin/env bash
set -euo pipefail

cat <<'EOF'
Election Platform demo reset guidance

Local demo mode
- Stop the running app.
- Restart with: ./scripts/start-demo-local.sh
- This resets in-memory demo users and returns the app to seeded candidate data.

Firestore-backed demo mode
- Delete documents from the configured collections:
  - candidates
  - votes
  - electionStats
  - voterProfiles
- Restart the app with Firestore startup seeding enabled.
- The app will recreate the seeded candidates and initial statistics document on startup.

Marker note
- If Firestore is unavailable, the app falls back to safe empty-state or local seeded presentation paths rather than exposing raw errors.
EOF
