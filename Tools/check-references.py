#!/usr/bin/env python3
"""Detects dangling asset references in scenes, prefabs and assets.

Why this exists
---------------
Renaming, moving or deleting a script keeps the *reference* in every scene and prefab that used it.
Unity then shows "Missing (Mono Script)" on that component, which silently breaks prefabs that a
headless compile check cannot see. After a refactor this is the most likely silent regression.

This script collects every GUID known to the project (all .meta files under Assets/ and
Library/PackageCache/, i.e. project + package assets) and reports every GUID referenced by a
.unity / .prefab / .asset file that resolves to nothing.

Usage
-----
    python Tools/check-references.py                 # checks Assets/**.unity|prefab|asset
    python Tools/check-references.py path/to/File.unity

Exit code 0 means every reference resolves.
"""

from __future__ import annotations

import argparse
import glob
import os
import re
import sys

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

GUID_PATTERN = re.compile(r"guid:\s*([0-9a-fA-F]{32})")
EXTENSIONS = (".unity", ".prefab", ".asset", ".controller", ".mat", ".playable")


def known_guids() -> set[str]:
    guids: set[str] = set()

    for root in (os.path.join(PROJECT, "Assets"),
                 os.path.join(PROJECT, "Packages"),
                 os.path.join(PROJECT, "Library", "PackageCache")):
        if not os.path.isdir(root):
            continue

        for path in glob.glob(os.path.join(root, "**", "*.meta"), recursive=True):
            try:
                with open(path, "r", encoding="utf-8", errors="ignore") as handle:
                    for line in handle:
                        if line.startswith("guid:"):
                            guids.add(line.split(":", 1)[1].strip())
                            break
            except OSError:
                continue

    return guids


def is_builtin(guid: str) -> bool:
    """Unity's built-in resources (default materials, fonts...) have zero-filled GUIDs."""
    return guid.startswith("0000000000000000")


def package_referenced_guids() -> set[str]:
    """GUIDs that also appear in package-authored files.

    Some assets stored in the project reference resources Unity resolves from a package or from the
    Editor's built-in library (URP volume profiles and renderers are the common case). Those GUIDs
    have no .meta inside the project, so treating them as broken would produce noise. If a package
    file references the same GUID, it is one of those shared resources.
    """
    guids: set[str] = set()

    for root in (os.path.join(PROJECT, "Library", "PackageCache"),
                 os.path.join(PROJECT, "Packages")):
        if not os.path.isdir(root):
            continue

        for path in glob.glob(os.path.join(root, "**", "*"), recursive=True):
            if not os.path.isfile(path):
                continue
            if path.endswith(".meta"):
                continue

            try:
                with open(path, "r", encoding="utf-8", errors="ignore") as handle:
                    guids.update(GUID_PATTERN.findall(handle.read()))
            except (OSError, UnicodeDecodeError):
                continue

    return guids


def check(files: list[str], guids: set[str], package_guids: set[str]) -> int:
    problems = 0

    for path in files:
        if not os.path.isfile(path):
            print(f"!! missing file: {path}")
            problems += 1
            continue

        with open(path, "r", encoding="utf-8", errors="ignore") as handle:
            content = handle.read()

        dangling: dict[str, int] = {}
        package_level: dict[str, int] = {}

        for guid in GUID_PATTERN.findall(content):
            if is_builtin(guid) or guid in guids:
                continue

            bucket = package_level if guid in package_guids else dangling
            bucket[guid] = bucket.get(guid, 0) + 1

        rel = os.path.relpath(path, PROJECT)

        if not dangling and not package_level:
            print(f"OK   {rel}")
            continue

        if dangling:
            problems += len(dangling)
            print(f"FAIL {rel}: {len(dangling)} unresolved reference(s)")

            for guid, count in sorted(dangling.items(), key=lambda item: -item[1]):
                print(f"       {guid}  ({count}x)")

        if package_level:
            print(f"WARN {rel}: {len(package_level)} reference(s) resolved by a package/built-in resource")

            for guid, count in sorted(package_level.items(), key=lambda item: -item[1]):
                print(f"       {guid}  ({count}x)")

    return problems


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("paths", nargs="*", help="files to check (default: all assets under Assets/)")
    args = parser.parse_args()

    if args.paths:
        files = [os.path.abspath(p) for p in args.paths]
    else:
        files = []
        for extension in EXTENSIONS:
            files.extend(glob.glob(os.path.join(PROJECT, "Assets", "**", f"*{extension}"), recursive=True))
        files = sorted(set(files))

    guids = known_guids()
    package_guids = package_referenced_guids()
    print(f"known GUIDs: {len(guids)}  |  files checked: {len(files)}")
    print()

    problems = check(files, guids, package_guids)

    print()
    print("all references resolve" if problems == 0 else f"{problems} dangling reference(s) found")

    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
