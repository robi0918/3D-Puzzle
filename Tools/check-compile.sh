#!/usr/bin/env bash
# Offline compile check for the Unity runtime scripts (Assembly-CSharp set).
# Uses Unity's own Roslyn (csc.dll) + this project's compiled package
# assemblies, so it reports the same CS errors the Unity Console would show,
# without needing to open the Editor.
#
# Usage:  bash Tools/check-compile.sh [source-root ...]
# Default source roots: Assets/_Project  +  Assets/ThirdParty/StarterAssets

set -u
w() { cygpath -w "$1"; }

PROJ="$(cd "$(dirname "$0")/.." && pwd)"
UNITY_EDITOR="${UNITY_EDITOR_DIR:-/c/Program Files/Unity/Hub/Editor/6000.3.7f1/Editor/Data}"
CSC="$UNITY_EDITOR/DotNetSdkRoslyn/csc.dll"
OUT_DIR="${LOCALAPPDATA:-$HOME}/Temp/unity-compile-check"
mkdir -p "$OUT_DIR"

ROOTS=("$@")
if [ ${#ROOTS[@]} -eq 0 ]; then
  if [ -d "$PROJ/Assets/_Project" ]; then ROOTS=("$PROJ/Assets/_Project")
  else ROOTS=("$PROJ/Assets/Scripts"); fi
fi
[ -d "$PROJ/Assets/ThirdParty/StarterAssets" ] && ROOTS+=("$PROJ/Assets/ThirdParty/StarterAssets")

# ---- source files (skip Editor-only folders and test assemblies) --------
: > "$OUT_DIR/sources.txt"
for r in "${ROOTS[@]}"; do
  find "$r" -name '*.cs' ! -path '*/Editor/*' ! -path '*/Tests/*' | sort | while read -r f; do
    echo "\"$(w "$f")\"" >> "$OUT_DIR/sources.txt"
  done
done

# ---- response file ------------------------------------------------------
RSP="$OUT_DIR/refs.rsp"
: > "$RSP"
{
  echo "-noconfig"
  echo "-nostdlib+"
  echo "-target:library"
  echo "-nologo"
  echo "-langversion:9.0"
  echo "-nowarn:0169,0649,0414,0067"
  echo "-define:ENABLE_INPUT_SYSTEM;UNITY_6000_0_OR_NEWER;UNITY_2023_1_OR_NEWER;UNITY_2022_2_OR_NEWER;UNITY_2021_3_OR_NEWER;UNITY_EDITOR_WIN"
  echo "-out:\"$(w "$OUT_DIR/check.dll")\""
} >> "$RSP"

for bcl in "NetStandard/ref/2.1.0/netstandard.dll" \
           "MonoBleedingEdge/lib/mono/unityjit-win32/Facades/System.Runtime.dll" \
           "MonoBleedingEdge/lib/mono/unityjit-win32/Facades/System.Threading.Tasks.dll"; do
  [ -f "$UNITY_EDITOR/$bcl" ] && echo "-r:\"$(w "$UNITY_EDITOR/$bcl")\"" >> "$RSP"
done

for f in "$UNITY_EDITOR"/Managed/UnityEngine/*.dll; do
  case "$(basename "$f")" in UnityEditor*) continue ;; esac   # player assemblies must not see these
  echo "-r:\"$(w "$f")\"" >> "$RSP"
done

# Assemblies built from this project's own sources are recompiled below, so referencing the copies
# Unity produced in Library/ScriptAssemblies would only cause duplicate-type errors (CS0436).
OWN_ASSEMBLIES=()
while IFS= read -r asmdef; do
  name=$(sed -n 's/.*"name"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' "$asmdef" | head -1)
  [ -n "$name" ] && OWN_ASSEMBLIES+=("$name.dll")
done < <(find "$PROJ/Assets" -name '*.asmdef' -not -path '*/Plugins/*' | sort)

for f in "$PROJ"/Library/ScriptAssemblies/*.dll; do
  base="$(basename "$f")"
  case "$base" in
    Assembly-CSharp*|*Editor*.dll|*Test*.dll|*DocCodeSamples*|*CodeGen*) continue ;;
  esac

  skip=0
  for own in "${OWN_ASSEMBLIES[@]:-}"; do
    [ "$base" = "$own" ] && skip=1 && break
  done
  [ "$skip" = 1 ] && continue

  echo "-r:\"$(w "$f")\"" >> "$RSP"
done

cat "$OUT_DIR/sources.txt" >> "$RSP"

echo "source files: $(wc -l < "$OUT_DIR/sources.txt") | references: $(grep -c '^-r:' "$RSP")"
dotnet exec "$(w "$CSC")" "@$(w "$RSP")" 2>&1 | sed 's/\r$//'
echo "exit=${PIPESTATUS[0]}"
