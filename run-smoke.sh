#!/usr/bin/env bash
# Builds the smoke app and runs every case on an Apple Silicon simulator.
# The app renders one MudBlazor component at a time, pokes it, and logs the outcome; after a crash or hang this relaunches it from the next case.
# Extra arguments go to dotnet build, e.g. -p:MtouchInterpreter=all for the control run.
set -euo pipefail
root=$(cd "$(dirname "$0")" && pwd)
config=${CONFIG:-Release}
logs=${LOG_DIR:-$root/logs/smoke}
mkdir -p "$logs"

cd "$root/smoke"
dotnet build MudSmoke.csproj -c "$config" -r iossimulator-arm64 "$@"
app="$root/smoke/bin/$config/net10.0-ios/iossimulator-arm64/MudSmoke.app"
bundle=$(/usr/libexec/PlistBuddy -c 'Print CFBundleIdentifier' "$app/Info.plist")
# The executable holds the AOT-compiled code, so its size shows which assemblies were interpreted.
executable=$(/usr/libexec/PlistBuddy -c 'Print CFBundleExecutable' "$app/Info.plist")
echo "App executable: $(du -k "$app/$executable" | cut -f1) KB"

# A plain numbered iPhone, unless SIM_DEVICE is set; "iPhone Duo" shut down on every launch. Plain statements because macOS runs bash 3.2.
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

# Launches from case $1 and waits until the runner finishes, the app exits, or no case makes progress for 45 seconds.
launch() {
  local start=$1 log=$2 launcher outcome=hang count last=-1 idle=0
  xcrun simctl bootstatus "$device" -b >/dev/null 2>&1 || true
  SIMCTL_CHILD_SMOKE_START="$start" \
    xcrun simctl launch --console --terminate-running-process "$device" "$bundle" > "$log" 2>&1 &
  launcher=$!
  while true; do
    sleep 1
    if grep -q 'SMOKE|done|' "$log"; then outcome=done; break; fi
    if ! kill -0 "$launcher" 2>/dev/null; then outcome=exited; break; fi
    count=$(grep -c 'SMOKE|' "$log" || true)
    if [ "$count" != "$last" ]; then last=$count; idle=0; else idle=$((idle + 1)); fi
    if [ "$idle" -ge 45 ]; then break; fi
  done
  xcrun simctl terminate "$device" "$bundle" 2>/dev/null || true
  wait "$launcher" 2>/dev/null || true
  # The console relay drops output, especially when the app aborts; the simulator's unified log keeps it.
  local pid
  pid=$(grep -oE "^$bundle: [0-9]+" "$log" | grep -oE '[0-9]+$' | tail -1 || true)
  if [ -n "$pid" ]; then
    { echo "--- unified log for pid $pid"; xcrun simctl spawn "$device" log show --style compact --last 10m --predicate "processID == $pid"; } >> "$log" 2>&1 || true
  fi
  echo "SMOKE-OUTCOME $outcome" >> "$log"
}

# The first launch after boot has been killed before app code ran, so it doesn't count.
launch 1000 "$logs/warm-up.log"

start=0
n=0
stalls=0
while [ "$start" -lt 1000 ] && [ "$n" -lt 80 ]; do
  log=$(printf '%s/launch-%02d.log' "$logs" "$n")
  launch "$start" "$log"
  next=$(python3 "$root/smoke-parse.py" next "$start" "$log")
  echo "launch $n from case $start: next $next"
  if [ "$next" = "$start" ]; then
    # The runner never started a case; retry once, then skip it.
    stalls=$((stalls + 1))
    if [ "$stalls" -ge 2 ]; then
      echo "SMOKE|no-start|$start||app never reached the runner" >> "$log"
      next=$((start + 1))
      stalls=0
    fi
  else
    stalls=0
  fi
  start=$next
  n=$((n + 1))
done

python3 "$root/smoke-parse.py" summary "$logs"/launch-*.log > "$logs/summary.md"
cat "$logs/summary.md"
if [ -n "${GITHUB_STEP_SUMMARY:-}" ]; then
  { echo "### Smoke, ${LABEL:-MudBlazor 9.12.0}, $config ${*:-full AOT}"; echo; cat "$logs/summary.md"; } >> "$GITHUB_STEP_SUMMARY"
fi
