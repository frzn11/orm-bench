#!/usr/bin/env bash
set -euo pipefail

# Path to your existing runner (the script you pasted)
RUNNER="${RUNNER:-./scripts/run-one.sh}"

# Matrices
# ORMS=(dapper efcore nhib)                # 3
# OPS=(getall getbyid update create delete) # 5
# SIZES=(small medium large)                # 3

ORMS=(nhib)                # 3
OPS=(create delete) # 5
SIZES=(small medium large)                # 3


# Pass-through / defaults
REPS="${REPS:-1}"
SAPWD="${SAPWD:-YourStrong!Passw0rd}"

# Optional: small pause between runs to reduce thermal carryover (seconds)
SLEEP_BETWEEN="${SLEEP_BETWEEN:-0}"

total=$(( ${#ORMS[@]} * ${#OPS[@]} * ${#SIZES[@]} ))
i=0

echo "Running $total combinations (REPS=$REPS)…"
for orm in "${ORMS[@]}"; do
  for op in "${OPS[@]}"; do
    for size in "${SIZES[@]}"; do
      i=$((i+1))
      echo
      echo "[$i/$total] >>> ORM=$orm OP=$op SIZE=$size REPS=$REPS"
      ORM="$orm" OP="$op" SIZE="$size" REPS="$REPS" SAPWD="$SAPWD" bash "$RUNNER"
      if [[ "$SLEEP_BETWEEN" != "0" ]]; then sleep "$SLEEP_BETWEEN"; fi
    done
  done
done

echo
echo "✓ All $total runs complete."
