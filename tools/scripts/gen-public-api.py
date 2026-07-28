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


REPO_ROOT = pathlib.Path(__file__).resolve().parents[2]


def resolve_within_repo(raw: str) -> pathlib.Path:
    """Resolve `raw` and reject anything that escapes the repository root.

    This script only ever reads a build's SARIF log and writes a `PublicAPI` baseline,
    both of which live inside the repository. Confining the arguments keeps a stray
    relative path from reading or overwriting an unrelated file on the machine.
    """
    resolved = pathlib.Path(raw).expanduser().resolve()
    if resolved != REPO_ROOT and REPO_ROOT not in resolved.parents:
        raise ValueError(f"path escapes the repository root: {raw}")
    return resolved


def main() -> int:
    if len(sys.argv) != 3:
        print(__doc__, file=sys.stderr)
        return 2

    try:
        sarif_path = resolve_within_repo(sys.argv[1])
        out_path = resolve_within_repo(sys.argv[2])
    except ValueError as ex:
        print(ex, file=sys.stderr)
        return 2

    log = json.loads(sarif_path.read_text(encoding="utf-8"))

    names = set()
    for run in log.get("runs", []):
        for result in run.get("results", []):
            if result.get("ruleId") != "RS0016":
                continue
            api = result.get("properties", {}).get("customProperties", {}).get("APIName")
            if api:
                names.add(api)

    target = out_path
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
