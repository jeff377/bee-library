#!/usr/bin/env python3
"""Fill a PublicAPI baseline from a build's SARIF diagnostics.

Reads RS0016 ("not part of the declared public API") results out of a SARIF log and
merges their APIName into the target baseline file, keeping any entries already there.

Only needed for a bulk rebuild of the baselines — day to day, paste the single line the
build error gives you into PublicAPI.Unshipped.txt. See docs/repo-ops/public-api-baseline.md.

Usage: gen-public-api.py <project.sarif> <path/to/PublicAPI.Shipped.txt>
"""
import json
import pathlib
import sys


def main() -> int:
    if len(sys.argv) != 3:
        print(__doc__, file=sys.stderr)
        return 2

    sarif_path, out_path = sys.argv[1], sys.argv[2]
    log = json.loads(pathlib.Path(sarif_path).read_text(encoding="utf-8"))

    names = set()
    for run in log.get("runs", []):
        for result in run.get("results", []):
            if result.get("ruleId") != "RS0016":
                continue
            api = result.get("properties", {}).get("customProperties", {}).get("APIName")
            if api:
                names.add(api)

    target = pathlib.Path(out_path)
    if target.exists():
        names.update(
            line for line in target.read_text(encoding="utf-8").splitlines()
            if line.strip() and not line.startswith("#")
        )

    body = sorted(names)
    target.write_text(
        "#nullable enable\n" + "\n".join(body) + ("\n" if body else ""),
        encoding="utf-8",
    )
    print(f"{target}: {len(body)} entries")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
