#!/usr/bin/env bash
# Builds the iOS host with Mono full AOT and runs each case in a fresh launch on an Apple Silicon simulator.
# Extra arguments go to dotnet build, e.g. bash run-sim.sh -p:MtouchInterpreter=all for the control run.
set -euo pipefail
cd "$(dirname "$0")"

# The gist copy is flat, so lay the files out as projects on first run.
if [ ! -d Issue14134.Ios ]; then
  mkdir -p Issue14134.Shapes
  cp Issue14134.Shapes.csproj Shapes.cs Issue14134.Shapes/
  dotnet new ios -n Issue14134.Ios -o Issue14134.Ios
  cp Issue14134.Ios.csproj SceneDelegate.cs Issue14134.Ios/
fi

config=${CONFIG:-Debug}
cases=(svm-async-int svm-async-double svm-async-int-direct nosvm-async-int svm-sync-int nosvm-sync-int svm-async-long-shared nosvm-async-long-shared sat-async-long-shared)
logs=${LOG_DIR:-logs}
mkdir -p "$logs"

dotnet build Issue14134.Ios/Issue14134.Ios.csproj -c "$config" -r iossimulator-arm64 "$@"
app="Issue14134.Ios/bin/$config/net10.0-ios/iossimulator-arm64/Issue14134.Ios.app"
bundle=$(/usr/libexec/PlistBuddy -c 'Print CFBundleIdentifier' "$app/Info.plist")

# Newest runtime's last iPhone, unless SIM_DEVICE is set.
device=${SIM_DEVICE:-$(xcrun simctl list devices available | grep 'iPhone' | tail -1 | grep -oE '[0-9A-F]{8}-([0-9A-F]{4}-){3}[0-9A-F]{12}')}
echo "Simulator: $(xcrun simctl list devices available | grep "$device" | xargs)"
xcrun simctl boot "$device" 2>/dev/null || true
xcrun simctl bootstatus "$device" -b >/dev/null
xcrun simctl install "$device" "$app"

summary=$logs/summary.md
printf '| Case | Result | Detail |\n| --- | --- | --- |\n' > "$summary"
for c in "${cases[@]}"; do
  # perl's alarm stands in for timeout(1), which macOS lacks.
  SIMCTL_CHILD_I14134_CASE="$c" perl -e 'alarm shift; exec @ARGV' 180 \
    xcrun simctl launch --console --terminate-running-process "$device" "$bundle" > "$logs/$c.log" 2>&1 || true
  if grep -q "I14134 ok $c" "$logs/$c.log"; then
    result=pass
    detail=
  else
    result=FAIL
    # The console relay drops output when the app aborts; the simulator's unified log keeps it.
    pid=$(grep -oE "^$bundle: [0-9]+" "$logs/$c.log" | grep -oE '[0-9]+$' | tail -1 || true)
    if [ -n "$pid" ]; then
      { echo "--- unified log for pid $pid"; xcrun simctl spawn "$device" log show --style compact --last 5m --predicate "processID == $pid"; } >> "$logs/$c.log" 2>&1 || true
    fi
    detail=$(grep -m1 -E 'Assertion|Attempting to JIT|I14134 error' "$logs/$c.log" | cut -c1-300 || true)
  fi
  echo "$c: $result${detail:+ - $detail}"
  echo "| \`$c\` | $result | ${detail//|/\\|} |" >> "$summary"
done

runtime=$(grep -h -m1 'I14134 runtime' "$logs"/*.log | head -1 || true)
echo "${runtime:-No runtime line: the app never reached the case runner.}"
if [ -n "${GITHUB_STEP_SUMMARY:-}" ]; then
  { echo "### $config ${*:-full AOT}"; echo; echo "${runtime:-No runtime line}"; echo; cat "$summary"; } >> "$GITHUB_STEP_SUMMARY"
fi
