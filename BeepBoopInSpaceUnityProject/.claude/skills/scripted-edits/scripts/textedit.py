#!/usr/bin/env python3
"""Text edits by script for Beep Boop dans l'espace, keeping each file's BOM and line endings.
Python 3 standard library only.

As a module, from a script written with the Write tool:

    import sys; sys.path.insert(0, ".claude/skills/scripted-edits/scripts")
    from textedit import edit, to_crlf
    edit("Assets/Game/X.cs", [(old, new), ...])      # each old text must be found exactly once
    edit("Assets/Game/X.cs", [(old, new)], count=0)   # every occurrence

Lines of a pattern match whatever trailing spaces the file has: the Write tool strips them from the
script, the files keep them. Paths are relative to the Unity project (or absolute).

As a command:

  eol [--fix] [paths...]       line endings of the changed and new text files against HEAD; --fix gives
                               them back HEAD's (new .cs: CRLF, the rest: LF)
  stage <path> <content file>  stages that content for <path> without touching the working copy (one
                               subject of a file that mixes two, see the parallel-sessions skill)
"""
import argparse
import re
import subprocess
import sys
from pathlib import Path

PROJECT = Path(__file__).resolve().parents[4]
TEXT_SUFFIXES = {".cs", ".uxml", ".uss", ".tss", ".md", ".py", ".json", ".asset", ".prefab", ".unity", ".meta",
                 ".mat", ".asmdef", ".txt", ".yml", ".yaml", ".shader", ".hlsl", ".mixer", ".controller", ".anim"}
# What git stores for files new to it: C# in CRLF like most of the project's, Unity YAML and the rest in LF.
NEW_FILE_CRLF = {".cs"}

if sys.stdout.encoding and sys.stdout.encoding.lower() != "utf-8":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")


def path_in_project(path):
    path = Path(path)
    return path if path.is_absolute() else PROJECT / path


def _pattern(text):
    lines = text.replace("\r\n", "\n").split("\n")
    return re.compile("[ \\t]*\n".join(re.escape(line.rstrip(" \t")) for line in lines))


def edit(path, pairs, count=1):
    """Replaces each (old, new) in the file. count=1: old must be there exactly once; 0: every occurrence."""
    full = path_in_project(path)
    raw = full.read_bytes()
    bom = raw.startswith(b"\xef\xbb\xbf")
    text = raw.decode("utf-8-sig")
    crlf = "\r\n" in text
    text = text.replace("\r\n", "\n")
    for old, new in pairs:
        pattern = _pattern(old)
        found = len(pattern.findall(text))
        if found == 0:
            raise SystemExit(f"Not found in {path}:\n{old[:300]}")
        if count == 1 and found > 1:
            raise SystemExit(f"Found {found} times in {path} (give more context, or count=0):\n{old[:300]}")
        new = new.replace("\r\n", "\n")
        text = pattern.sub(lambda _: new, text, count=1 if count == 1 else 0)
    if crlf:
        text = text.replace("\n", "\r\n")
    full.write_bytes((b"\xef\xbb\xbf" if bom else b"") + text.encode("utf-8"))
    print(f"edited {path}")


def _set_endings(full, crlf):
    text = full.read_bytes().decode("utf-8").replace("\r\n", "\n")
    full.write_bytes((text.replace("\n", "\r\n") if crlf else text).encode("utf-8"))


def to_crlf(path):
    _set_endings(path_in_project(path), True)


def to_lf(path):
    _set_endings(path_in_project(path), False)


def git(*args, input_bytes=None):
    return subprocess.run(["git", *args], cwd=PROJECT, input=input_bytes, capture_output=True, check=True).stdout


def cmd_eol(args):
    changed = git("diff", "--name-only", "--relative", "HEAD", "--", *args.paths).decode().split()
    new = git("ls-files", "--others", "--exclude-standard", "--", *args.paths).decode().split()
    wrong = []
    for path in changed + new:
        full = PROJECT / path
        if full.suffix not in TEXT_SUFFIXES or not full.is_file():
            continue
        has_crlf = b"\r\n" in full.read_bytes()
        if path in changed:
            head = git("ls-files", "--eol", "--", path).decode().split()
            wants_crlf = bool(head) and head[0] == "i/crlf"
            if not head or head[0] not in ("i/crlf", "i/lf"):
                continue
        else:
            wants_crlf = full.suffix in NEW_FILE_CRLF
        if has_crlf != wants_crlf:
            wrong.append((path, wants_crlf))
    for path, wants_crlf in wrong:
        print(f"{'CRLF' if wants_crlf else 'LF  '} expected: {path}")
        if args.fix:
            _set_endings(PROJECT / path, wants_crlf)
    if not wrong:
        print("Line endings as in HEAD.")
    elif args.fix:
        print(f"{len(wrong)} file(s) fixed.")
    return 0


def cmd_stage(args):
    content = Path(args.content).read_bytes()
    blob = git("hash-object", "-w", "--stdin", "--path", args.path, input_bytes=content).decode().strip()
    prefix = git("rev-parse", "--show-prefix").decode().strip()
    git("update-index", "--add", "--cacheinfo", f"100644,{blob},{prefix}{args.path}")
    print(f"staged {args.path} ({blob[:8]}), working copy untouched: check with `git diff --cached -- {args.path}`")
    return 0


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="command", required=True)
    p = sub.add_parser("eol", help="line endings of changed and new files against HEAD")
    p.add_argument("paths", nargs="*")
    p.add_argument("--fix", action="store_true")
    p.set_defaults(func=cmd_eol)
    p = sub.add_parser("stage", help="stage a content for a path, without touching the working copy")
    p.add_argument("path", help="path relative to the Unity project")
    p.add_argument("content", help="file holding the content to stage")
    p.set_defaults(func=cmd_stage)
    args = parser.parse_args()
    sys.exit(args.func(args))


if __name__ == "__main__":
    main()
