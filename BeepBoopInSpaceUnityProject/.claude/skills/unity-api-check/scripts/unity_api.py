#!/usr/bin/env python3
"""Unity API of the editor version the project uses, for Beep Boop dans l'espace. Python 3 standard library only.

  find <regex>      types and members whose full name matches (case-insensitive), with their summary
  type <Type>       a type's summary and all its documented members
  source <Type>     where a type is declared in the project's packages (Library/PackageCache), with its file
  version           editor version and where the docs are read from

The docs are the XML files the editor ships next to its assemblies: only documented public API. A member that
is missing may still exist (quick-compile decides), one that is listed exists in this exact version.
Engine code that no package contains: UnityCsReference on GitHub (its master can be newer than the project).
"""
import argparse
import html
import re
import sys
from pathlib import Path

sys.dont_write_bytecode = True  # no __pycache__ next to unity_cli.py
sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "unity-headless" / "scripts"))
import unity_cli  # noqa: E402  (project root, editor version and Unity install lookup)

PROJECT = unity_cli.PROJECT
MEMBER_RE = re.compile(r'<member name="([TMPFE]):([^"]+)">(.*?)</member>', re.S)
KINDS = {"T": "type", "M": "method", "P": "property", "F": "field", "E": "event"}
CS_REFERENCE = "https://github.com/Unity-Technologies/UnityCsReference"


def managed_dir():
    return unity_cli.unity_exe().parent / "Data" / "Managed"


def doc_files():
    managed = managed_dir()
    return sorted(managed.glob("*.xml")) + sorted((managed / "UnityEngine").glob("*.xml"))


def members():
    # UnityEngine.xml repeats what the module files document: each member once.
    seen = set()
    for path in doc_files():
        text = path.read_text(encoding="utf-8", errors="replace")
        for match in MEMBER_RE.finditer(text):
            key = (match.group(1), match.group(2))
            if key not in seen:
                seen.add(key)
                yield match.group(1), match.group(2), match.group(3), path.stem


def summary(raw):
    text = re.sub(r'<see cref="\w:([^"]+)"\s*/>', lambda m: m.group(1).split(".")[-1], raw)
    text = re.sub(r'<paramref name="([^"]+)"\s*/>', r"\1", text)
    text = re.sub(r"<param name=\"([^\"]+)\">", r" [\1] ", text)
    text = re.sub(r"<returns>", " Returns: ", text)
    text = re.sub(r"<[^>]+>", " ", text)
    return " ".join(html.unescape(text).split())


def short_name(full):
    return re.sub(r"^UnityEngine\.(UIElements\.|InputSystem\.)?|^UnityEditor\.", "", full)


def cmd_find(args):
    pattern = re.compile(args.regex, re.I)
    found = [(kind, name, raw, module) for kind, name, raw, module in members() if pattern.search(name)]
    for kind, name, raw, module in found[:args.limit]:
        print(f"{KINDS[kind]:8} {short_name(name)}  [{module}]\n         {summary(raw)[:220]}")
    if len(found) > args.limit:
        print(f"... {len(found) - args.limit} more: narrow the regex or raise --limit.")
    if not found:
        print("Nothing documented matches. The API may not exist in this version, or be internal: "
              "`source` searches the packages, quick-compile has the final word.")
    return 0 if found else 1


def cmd_type(args):
    wanted = args.type.replace("<", "`").rstrip(">")
    types = [(name, raw, module) for kind, name, raw, module in members()
             if kind == "T" and (name == wanted or name.endswith("." + wanted) or name.split(".")[-1].split("`")[0] == wanted)]
    if not types:
        print(f"No documented type '{args.type}'. Try `find {args.type}` or `source {args.type}`.")
        return 1
    for name, raw, module in types:
        print(f"{name}  [{module}]\n  {summary(raw)}")
        prefix = name.replace("`", "_") + "."
        rows = sorted((kind, member, raw) for kind, member, raw, _ in members()
                      if kind != "T" and member.replace("`", "_").startswith(prefix))
        for kind, member, raw in rows:
            print(f"  {KINDS[kind]:8} {member[len(prefix):][:90]}\n           {summary(raw)[:200]}")
    return 0


def cmd_source(args):
    pattern = re.compile(r"^\s*(?:\[[^\]]*\]\s*)*(?:public|internal|protected|private|static|sealed|abstract|partial|readonly|\s)*"
                         r"(?:class|struct|interface|enum|record)\s+" + re.escape(args.type) + r"\b", re.M)
    roots = (PROJECT / "Library" / "PackageCache", PROJECT / "Packages", PROJECT / "Assets")
    hits = 0
    # A type usually lives in the file of its name: every file is read only when that finds nothing.
    for glob in (f"{args.type}.cs", "*.cs"):
        for root in roots:
            for path in root.rglob(glob):
                try:
                    raw = path.read_bytes()
                except OSError:
                    continue
                if args.type.encode() not in raw:
                    continue
                text = raw.decode("utf-8", errors="replace")
                for match in pattern.finditer(text):
                    line = text.count("\n", 0, match.start()) + 1
                    print(f"{path.relative_to(PROJECT).as_posix()}:{line}")
                    hits += 1
        if hits:
            break
    if not hits:
        print(f"'{args.type}' isn't declared in the project's packages: it is engine code. Read it in {CS_REFERENCE} "
              f"(search the type name; raw files: https://raw.githubusercontent.com/Unity-Technologies/UnityCsReference/master/<path>). "
              "Its master branch may be newer than the project's version.")
    return 0 if hits else 1


def cmd_version(args):
    print(f"Project editor: {unity_cli.editor_version()}\nDocs: {managed_dir()} ({len(doc_files())} XML files)")
    return 0


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="command", required=True)
    p = sub.add_parser("find", help="types and members whose full name matches a regex")
    p.add_argument("regex")
    p.add_argument("--limit", type=int, default=40)
    p.set_defaults(func=cmd_find)
    p = sub.add_parser("type", help="a type and its members")
    p.add_argument("type")
    p.set_defaults(func=cmd_type)
    p = sub.add_parser("source", help="where a type is declared in the packages")
    p.add_argument("type")
    p.set_defaults(func=cmd_source)
    sub.add_parser("version", help="editor version and docs location").set_defaults(func=cmd_version)
    args = parser.parse_args()
    sys.exit(args.func(args))


if __name__ == "__main__":
    main()
