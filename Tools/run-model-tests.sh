#!/usr/bin/env bash
# Runs the puzzle rule checks OUTSIDE Unity.
#
# The two puzzle models (MemoryMatchModel, SlidingPuzzleModel) are plain C# with no engine
# dependency, so they are compiled straight from the project sources and executed on the Mono
# runtime that ships with the Unity Editor. No Editor, no scene, no build required.
#
# Usage:  bash Tools/run-model-tests.sh
# Exit 0 = every rule check passed.

set -u

UNITY_EDITOR="${UNITY_EDITOR_DIR:-/c/Program Files/Unity/Hub/Editor/6000.3.7f1/Editor/Data}"
CSC="$UNITY_EDITOR/DotNetSdkRoslyn/csc.dll"
MONO="$UNITY_EDITOR/MonoBleedingEdge/bin/mono.exe"
MONO_LIB="$UNITY_EDITOR/MonoBleedingEdge/lib/mono/unityjit-win32"

PROJ="$(cd "$(dirname "$0")/.." && pwd)"
OUT="${LOCALAPPDATA:-$HOME}/Temp/model-tests"
mkdir -p "$OUT"

w() { cygpath -w "$1"; }

if [ ! -f "$CSC" ] || [ ! -f "$MONO" ]; then
  echo "!! Unity toolchain not found under: $UNITY_EDITOR"
  echo "   Set UNITY_EDITOR_DIR to <UnityInstall>/Editor/Data and retry."
  exit 2
fi

RSP="$OUT/build.rsp"
: > "$RSP"
{
  echo "-noconfig"
  echo "-nostdlib+"
  echo "-target:exe"
  echo "-nologo"
  echo "-langversion:9.0"
  echo "-out:\"$(w "$OUT/ModelTests.exe")\""
  echo "-r:\"$(w "$MONO_LIB/mscorlib.dll")\""
  echo "-r:\"$(w "$MONO_LIB/System.dll")\""
  echo "-r:\"$(w "$MONO_LIB/System.Core.dll")\""
  echo "\"$(w "$PROJ/Tools/ModelTests/ModelTests.cs")\""
  echo "\"$(w "$PROJ/Assets/_Project/Scripts/Puzzle/MemoryMatch/MemoryMatchModel.cs")\""
  echo "\"$(w "$PROJ/Assets/_Project/Scripts/Puzzle/SlidingPuzzle/SlidingPuzzleModel.cs")\""
} >> "$RSP"

dotnet exec "$(w "$CSC")" "@$(w "$RSP")" || { echo "compile failed"; exit 1; }

"$(w "$MONO")" "$(w "$OUT/ModelTests.exe")"
echo "exit=$?"
