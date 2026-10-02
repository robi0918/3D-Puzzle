#!/usr/bin/env python3
"""Offline per-assembly-definition compile check for the Unity project.

Why this exists
---------------
`Tools/check-compile.sh` compiles every script into ONE assembly, which proves the code is
syntactically and semantically valid but hides two classes of error that only appear in Unity:

  1. an .asmdef that references an assembly name that does not exist (typo, renamed package,
     missing `Unity.TextMeshPro` style reference);
  2. a layering violation, i.e. assembly A using a type from assembly B without declaring it
     (Unity reports CS0246);
  3. reference cycles between assemblies.

This script reproduces Unity's own compilation model offline: it compiles each .asmdef on its
own, in dependency order, using ONLY the references that the .asmdef declares (plus the Unity
engine modules, which Unity always adds). The produced DLL of an earlier assembly is used as the
reference for later ones, exactly like Library/ScriptAssemblies.

Usage
-----
    python Tools/check-asmdefs.py            # check every assembly under Assets/
    python Tools/check-asmdefs.py --verbose

Exit code 0 means the assembly graph is exactly what Unity will build.
"""

from __future__ import annotations

import argparse
import glob
import json
import os
import subprocess
import sys

PROJECT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
UNITY_EDITOR_DIR = os.environ.get(
    "UNITY_EDITOR_DIR", r"C:\Program Files\Unity\Hub\Editor\6000.3.7f1\Editor\Data"
)
CSC = os.path.join(UNITY_EDITOR_DIR, "DotNetSdkRoslyn", "csc.dll")

OUT_DIR = os.path.join(os.environ.get("TEMP", "/tmp"), "unity-asmdef-check")

# Assemblies Unity builds from the project's own sources; never usable as a reference target
# from an assembly definition.
PREDEFINED = {"Assembly-CSharp", "Assembly-CSharp-Editor", "Assembly-CSharp-firstpass"}


def find_asmdefs() -> dict[str, dict]:
    """name -> {path, dir, references, includePlatforms}

    Assembly definitions under Assets/Plugins (third-party packages shipped as source, e.g.
    UniTask) are prebuilt by Unity into Library/ScriptAssemblies and are resolved from there
    instead of being recompiled: recompiling them needs the exact package set their own
    versionDefines expect, which is noise for this check.
    """
    found: dict[str, dict] = {}

    for path in sorted(glob.glob(os.path.join(PROJECT, "Assets", "**", "*.asmdef"), recursive=True)):
        if os.sep + "Plugins" + os.sep in path:
            continue

        with open(path, "r", encoding="utf-8-sig") as handle:
            data = json.load(handle)

        name = data.get("name")
        if not name:
            print(f"!! {path}: no \"name\" field")
            continue

        if name in found:
            print(f"!! duplicate assembly definition name {name}: {path} and {found[name]['path']}")
            continue

        found[name] = {
            "path": path,
            "dir": os.path.dirname(path),
            "references": data.get("references") or [],
            "precompiled": data.get("precompiledReferences") or [],
            "includePlatforms": data.get("includePlatforms") or [],
        }

    return found


def find_in_package_cache(assembly_name: str) -> str | None:
    """Locates a precompiled assembly shipped inside an installed package (nunit.framework.dll...)."""
    matches = glob.glob(os.path.join(PROJECT, "Library", "PackageCache", "**", f"{assembly_name}.dll"),
                        recursive=True)
    return matches[0] if matches else None


def sources_for(asmdef: dict, all_defs: dict) -> list[str]:
    """All .cs under the asmdef folder, minus folders that host a nested asmdef."""
    nested_dirs = [
        other["dir"]
        for other in all_defs.values()
        if other["path"] != asmdef["path"]
        and os.path.abspath(other["dir"]).startswith(os.path.abspath(asmdef["dir"]) + os.sep)
    ]

    sources = []
    for root, _dirs, files in os.walk(asmdef["dir"]):
        if any(os.path.abspath(root).startswith(os.path.abspath(nested) + os.sep) or
               os.path.abspath(root) == os.path.abspath(nested) for nested in nested_dirs):
            continue

        for filename in files:
            if filename.endswith(".cs"):
                sources.append(os.path.join(root, filename))

    return sorted(sources)


def compile_order(defs: dict[str, dict]) -> list[str]:
    """Topological order over the references that are themselves project assemblies."""
    order: list[str] = []
    state: dict[str, int] = {}  # 0 = visiting, 1 = done

    def visit(name: str, chain: list[str]) -> None:
        if state.get(name) == 1:
            return
        if state.get(name) == 0:
            cycle = " -> ".join(chain + [name])
            raise SystemExit(f"!! reference cycle between assemblies: {cycle}")

        state[name] = 0
        for reference in defs[name]["references"]:
            if reference in defs:
                visit(reference, chain + [name])
        state[name] = 1
        order.append(name)

    for name in sorted(defs):
        visit(name, [])

    return order


def engine_references(include_editor: bool) -> list[str]:
    """Unity engine assemblies.

    `Editor/Data/Managed/UnityEngine/` also holds the editor modules (UnityEditor.dll,
    UnityEditor.CoreModule.dll, ...). Player assemblies must not see them, and an editor assembly
    must not reference both UnityEditor.dll and UnityEditor.CoreModule.dll — the same type would
    resolve twice (CS0433), which is why only one of the two reference sets is used per assembly.
    """
    refs: list[str] = []

    for path in sorted(glob.glob(os.path.join(UNITY_EDITOR_DIR, "Managed", "UnityEngine", "*.dll"))):
        if not include_editor and os.path.basename(path).startswith("UnityEditor"):
            continue

        refs.append(path)

    return refs


def bcl_references(legacy: bool) -> list[str]:
    """Base class library references.

    Most assemblies are compiled against Unity's netstandard profile. Assemblies that consume a
    precompiled DLL shipped inside a package (nunit.framework.dll for Unity test assemblies) are
    net4x/mono builds instead, so those are compiled against Unity's Mono mscorlib, which is what
    the Editor itself does.
    """
    if legacy:
        mono_dir = os.path.join(UNITY_EDITOR_DIR, "MonoBleedingEdge", "lib", "mono", "unityjit-win32")
        names = (
            "mscorlib.dll",
            "System.dll",
            "System.Core.dll",
            # The netstandard facade: without it, assemblies built against netstandard
            # (Game.Puzzle.dll) cannot be consumed by a mscorlib-based assembly.
            "Facades/netstandard.dll",
        )
        return [os.path.join(mono_dir, name) for name in names
                if os.path.isfile(os.path.join(mono_dir, name))]

    refs = []
    for relative in (
        r"NetStandard\ref\2.1.0\netstandard.dll",
        r"MonoBleedingEdge\lib\mono\unityjit-win32\Facades\System.Runtime.dll",
        r"MonoBleedingEdge\lib\mono\unityjit-win32\Facades\System.Threading.Tasks.dll",
    ):
        candidate = os.path.join(UNITY_EDITOR_DIR, relative)
        if os.path.isfile(candidate):
            refs.append(candidate)

    return refs


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--verbose", action="store_true")
    args = parser.parse_args()

    if not os.path.isfile(CSC):
        print(f"!! csc.dll not found at {CSC} (set UNITY_EDITOR_DIR)")
        return 2

    os.makedirs(OUT_DIR, exist_ok=True)

    defs = find_asmdefs()
    if not defs:
        print("!! no .asmdef files found")
        return 2

    # Pre-built package assemblies (Unity.TextMeshPro, UnityEngine.UI, UniTask, ...).
    script_assemblies = {
        os.path.splitext(os.path.basename(path))[0]: path
        for path in glob.glob(os.path.join(PROJECT, "Library", "ScriptAssemblies", "*.dll"))
        if os.path.splitext(os.path.basename(path))[0] not in PREDEFINED
    }

    package_cache_root = os.path.join(PROJECT, "Library", "PackageCache")
    produced: dict[str, str] = {}
    failed = 0

    for name in compile_order(defs):
        asmdef = defs[name]
        sources = sources_for(asmdef, defs)

        resolved_refs: list[str] = []
        missing: list[str] = []

        # Assemblies referenced by name, plus precompiled DLL references (nunit.framework.dll in
        # Unity test assemblies, for example).
        targets = list(asmdef["references"]) + [os.path.splitext(p)[0] for p in asmdef["precompiled"]]

        for reference in targets:
            resolved = produced.get(reference) or script_assemblies.get(reference)

            if resolved is None:
                resolved = find_in_package_cache(reference)

            if resolved is None:
                missing.append(reference)
            else:
                resolved_refs.append(resolved)

        if missing:
            print(f"!! {name}: unresolved reference(s) {missing}")
            failed += 1
            continue

        legacy_bcl = any(path.startswith(package_cache_root) for path in resolved_refs)
        is_editor = "Editor" in asmdef["includePlatforms"]
        refs = engine_references(is_editor) + bcl_references(legacy_bcl) + resolved_refs

        if not sources:
            print(f"?? {name}: no sources (skipped)")
            continue

        output = os.path.join(OUT_DIR, f"{name}.dll")
        response = os.path.join(OUT_DIR, f"{name}.rsp")

        with open(response, "w", encoding="utf-8") as handle:
            handle.write("-noconfig\n-nostdlib+\n-target:library\n-nologo\n-langversion:9.0\n")
            handle.write("-nowarn:0169,0649,0414,0067\n")
            handle.write(
                "-define:ENABLE_INPUT_SYSTEM;UNITY_6000_0_OR_NEWER;UNITY_2023_1_OR_NEWER;"
                "UNITY_2022_2_OR_NEWER;UNITY_2021_3_OR_NEWER;UNITY_EDITOR_WIN;UNITY_STANDALONE_WIN"
                + (";UNITY_EDITOR" if is_editor else "")
                + "\n"
            )
            handle.write(f'-out:"{output}"\n')
            for reference in refs:
                handle.write(f'-r:"{reference}"\n')
            for source in sources:
                handle.write(f'"{source}"\n')

        result = subprocess.run(
            ["dotnet", "exec", CSC, f"@{response}"],
            capture_output=True,
            text=True,
            cwd=PROJECT,
        )
        output_text = (result.stdout + result.stderr).replace("\r", "").strip()

        if result.returncode == 0:
            produced[name] = output
            status = f"OK   {name} ({len(sources)} files, {len(refs)} refs)"
            if args.verbose:
                status += f"\n       refs: {', '.join(asmdef['references']) or '(none)'}"
            print(status)
        else:
            failed += 1
            print(f"FAIL {name} ({len(sources)} files)")
            for line in output_text.splitlines():
                if ": error" in line:
                    print(f"       {line}")

    print(f"\nassemblies: {len(produced)} compiled, {failed} failed")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
