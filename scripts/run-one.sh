#!/usr/bin/env bash
set -euo pipefail

CSV="results/runs.csv"
DOTNET_PROJ="src/Bench/Bench.csproj"

ORM="${ORM:-dapper}"
OP="${OP:-getAll}"         # create|getById|getAll|update|delete
SIZE="${SIZE:-small}"      # small|medium|large
REPS="${REPS:-1}"
SAPWD="${SAPWD:-YourStrong!Passw0rd}"

mkdir -p "$(dirname "$CSV")"

# Ensure jq is available
command -v jq >/dev/null 2>&1 || { echo "jq not installed"; exit 1; }

# Write header if file doesn't exist yet
if [ ! -f "$CSV" ]; then
  echo "timestamp,orm,op,size,table,reps,duration_ms,rows_affected,energy_j" > "$CSV"
fi

# dotnet run --project "$DOTNET_PROJ" -c Release -- \
#   --orm "$ORM" --op "$OP" --size "$SIZE" --reps "$REPS" --sapwd "$SAPWD"

# # Run benchmark, capture JSON (stderr logs still show live)
JSON=$(dotnet run --project "$DOTNET_PROJ" -c Release -- \
  --orm "$ORM" --op "$OP" --size "$SIZE" --reps "$REPS" --sapwd "$SAPWD")

# Parse JSON with jq
TS=$(echo "$JSON" | jq -r '.timestamp')
ORMN=$(echo "$JSON" | jq -r '.orm')
OPN=$(echo "$JSON" | jq -r '.op')
SZN=$(echo "$JSON" | jq -r '.size')
TAB=$(echo "$JSON" | jq -r '.table')
REPSN=$(echo "$JSON" | jq -r '.reps')
DUR=$(echo "$JSON" | jq -r '.duration_ms')
ROWS=$(echo "$JSON" | jq -r '.rows_affected')
ENG=$(echo "$JSON" | jq -r '.energy_j')

# Append to CSV
echo "\"$TS\",\"$ORMN\",\"$OPN\",\"$SZN\",\"$TAB\",$REPSN,$DUR,$ROWS,$ENG" >> "$CSV"

echo "✔ $ORMN $OPN $SZN → $ROWS rows in ${DUR} ms, ${ENG} J"
