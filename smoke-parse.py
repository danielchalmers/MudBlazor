"""Reads the smoke app's logs.

next <start> <log>: prints the case to launch from next (1000 when the runner finished).
summary <logs...>: prints a Markdown table with each case's result.
"""
import re
import sys

LINE = re.compile(r"SMOKE\|([\w-]+)\|(-?\d+)\|([^|]*)\|(.*)$")
FAILURE = re.compile(r"Assertion at|Attempting to JIT|ExecutionEngineException|SIGABRT|SIGSEGV|Unhandled managed exception|Terminating app")


def read(path):
    events, outcome, failure = [], "exited", None
    seen = set()
    with open(path, encoding="utf-8", errors="replace") as handle:
        for raw in handle:
            line = raw.rstrip("\n")
            if line.startswith("SMOKE-OUTCOME "):
                outcome = line.split(" ", 1)[1]
                continue
            match = LINE.search(line)
            if match:
                phase, index, name, detail = match.group(1), int(match.group(2)), match.group(3), match.group(4).strip()
                # The console and the unified log both carry each line.
                key = (phase, index, name, detail[:80])
                if key not in seen:
                    seen.add(key)
                    events.append((phase, index, name, detail))
            elif failure is None and FAILURE.search(line):
                failure = line.strip()[:300]
    return events, outcome, failure


def next_start(start, path):
    events, _, _ = read(path)
    if any(event[0] == "done" for event in events):
        return 1000
    started = [event[1] for event in events if event[0] == "start"]
    return max(started) + 1 if started else start


def summary(paths):
    cases = {}
    for path in paths:
        events, outcome, failure = read(path)
        last_started = None
        for phase, index, name, detail in events:
            if phase == "start":
                cases[index] = {"name": name, "result": None, "stage": "render", "detail": "", "extra": []}
                last_started = index
            elif phase == "no-start":
                cases[index] = {"name": "?", "result": "no-start", "stage": "-", "detail": detail, "extra": []}
            elif index in cases:
                case = cases[index]
                if phase == "rendered":
                    case["stage"] = "poke"
                elif phase in ("ok", "error"):
                    case["result"] = phase
                    case["detail"] = detail
                elif phase in ("log", "unhandled", "unobserved"):
                    case["extra"].append(f"{phase}: {detail}")
        if last_started is not None and cases[last_started]["result"] is None:
            case = cases[last_started]
            case["result"] = "hang" if outcome == "hang" else "crash"
            case["detail"] = failure or "no output after the case started"

    rows = ["| # | Case | Result | Stage | Detail |", "| --- | --- | --- | --- | --- |"]
    counts = {}
    for index in sorted(cases):
        case = cases[index]
        result = case["result"] or "unknown"
        if result == "ok" and case["extra"]:
            result = "error"
        counts[result] = counts.get(result, 0) + 1
        parts = [] if result == "ok" else [case["detail"]]
        parts += case["extra"]
        detail = "; ".join(part for part in parts if part).replace("|", "/")[:400]
        stage = "" if result == "ok" else case["stage"]
        rows.append(f"| {index} | `{case['name']}` | {result} | {stage} | {detail} |")
    rows.append("")
    rows.append(", ".join(f"{key}: {value}" for key, value in sorted(counts.items())))
    return "\n".join(rows)


if __name__ == "__main__":
    if sys.argv[1] == "next":
        print(next_start(int(sys.argv[2]), sys.argv[3]))
    else:
        print(summary(sys.argv[2:]))
