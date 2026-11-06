#!/usr/bin/env bash
# Builds the reporter's MAUI app (techscex/MudBlazorTestOne) and drives its MudSlider page on an Apple Silicon simulator.
# Each case opens /matched-sliders with the app's own synthetic input driver, which moves one real MudSlider through 181, 220, 140, 260.
# Extra arguments go to dotnet build, e.g. -p:MtouchInterpreter=all for the control run.
set -euo pipefail
root=$(cd "$(dirname "$0")" && pwd)
app_dir=${APP_DIR:-$root/reporter}
config=${CONFIG:-Debug}
logs=${LOG_DIR:-$root/logs/reporter}
attempts=${ATTEMPTS:-2}
cases=(int-bind double-bind double-task double-task-label)
mkdir -p "$logs"

# The app's global.json picks the SDK and workload set, so build from its folder.
cd "$app_dir"
dotnet build MudSliderHybridRepro.csproj -c "$config" -r iossimulator-arm64 "$@"
app="$app_dir/bin/$config/net10.0-ios/iossimulator-arm64/MudSliderHybridRepro.app"
bundle=$(/usr/libexec/PlistBuddy -c 'Print CFBundleIdentifier' "$app/Info.plist")

# A plain numbered iPhone on the newest runtime, unless SIM_DEVICE is set; "iPhone Duo" shut down on every launch.
# Kept to plain statements because macOS runs this with bash 3.2.
device=${SIM_DEVICE:-}
if [ -z "$device" ]; then
  iphones=$(xcrun simctl list devices available | grep 'iPhone' || true)
  pick=$(echo "$iphones" | grep -E '^ +iPhone [0-9]+ [(]' | tail -1 || true)
  [ -n "$pick" ] || pick=$(echo "$iphones" | tail -1)
  device=$(echo "$pick" | grep -oE '[0-9A-F]{8}-([0-9A-F]{4}-){3}[0-9A-F]{12}')
fi
echo "Simulator: $(xcrun simctl list devices available | grep "$device" | xargs)"
xcrun simctl boot "$device" 2>/dev/null || true
xcrun simctl bootstatus "$device" -b >/dev/null
xcrun simctl install "$device" "$app"
if [ -f "$app/MudBlazor.dll" ]; then echo "App bundle MudBlazor.dll: $(shasum -a 256 "$app/MudBlazor.dll" | cut -d' ' -f1)"; fi

# Launches one case and waits for the driver to finish, the app to exit, or 90 seconds. Prints pass, wrong-value, crash, timeout or sim-error.
run_case() {
  local c=$1 log=$2 launcher
  xcrun simctl bootstatus "$device" -b >/dev/null 2>&1 || true
  SIMCTL_CHILD_REPRO_START_PATH="/matched-sliders?case=$c&drive=true" \
    xcrun simctl launch --console --terminate-running-process "$device" "$bundle" > "$log" 2>&1 &
  launcher=$!
  local outcome=timeout
  for _ in $(seq 1 90); do
    if grep -q "synthetic-completed|$c|" "$log"; then outcome=done; break; fi
    if ! kill -0 "$launcher" 2>/dev/null; then outcome=crash; break; fi
    sleep 1
  done
  xcrun simctl terminate "$device" "$bundle" 2>/dev/null || true
  wait "$launcher" 2>/dev/null || true
  if grep -q 'Unable to lookup in current state' "$log"; then echo sim-error; return; fi
  if [ "$outcome" = done ]; then
    # The driver's last input is 260; anything else means the binding didn't round-trip.
    if grep "synthetic-completed|$c|" "$log" | grep -q '"input":"260","observed":"260"'; then echo pass; else echo wrong-value; fi
    return
  fi
  # The console relay drops output when the app aborts; the simulator's unified log keeps it.
  local pid
  pid=$(grep -oE "^$bundle: [0-9]+" "$log" | grep -oE '[0-9]+$' | tail -1 || true)
  if [ -n "$pid" ]; then
    { echo "--- unified log for pid $pid"; xcrun simctl spawn "$device" log show --style compact --last 5m --predicate "processID == $pid"; } >> "$log" 2>&1 || true
  fi
  echo "$outcome"
}

# The first launch after boot has been killed before app code ran, so it doesn't count.
run_case int-bind "$logs/warm-up.log" > /dev/null

summary=$logs/summary.md
printf '| Case | Results | Detail |\n| --- | --- | --- |\n' > "$summary"
for c in "${cases[@]}"; do
  results=()
  for i in $(seq 1 "$attempts"); do
    results+=("$(run_case "$c" "$logs/$c-$i.log")")
  done
  detail=$(grep -h -m1 -E 'Assertion|Attempting to JIT|SIGABRT|SIGSEGV' "$logs/$c"-*.log | head -1 | cut -c1-300 || true)
  echo "$c: ${results[*]}${detail:+ - $detail}"
  echo "| \`$c\` | ${results[*]} | ${detail//|/\\|} |" >> "$summary"
done

runtime=$(ls "${DOTNET_ROOT:-$(dirname "$(command -v dotnet)")}/packs/Microsoft.NETCore.App.Runtime.Mono.iossimulator-arm64" 2>/dev/null | tr '\n' ' ' || true)
echo "Mono runtime packs: ${runtime:-unknown}"
if [ -n "${GITHUB_STEP_SUMMARY:-}" ]; then
  { echo "### Reporter app${LABEL:+ ($LABEL)}, $config ${*:-full AOT}"; echo; echo "SDK $(dotnet --version), Mono runtime pack ${runtime:-unknown}"; echo; cat "$summary"; } >> "$GITHUB_STEP_SUMMARY"
fi
