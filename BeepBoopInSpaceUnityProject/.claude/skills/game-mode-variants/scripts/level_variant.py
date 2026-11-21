#!/usr/bin/env python3
"""Game mode variants (LevelDataAsset) for Beep Boop dans l'espace. Python 3 standard library only.

  list                       building blocks (objectives, special actions, modifiers) and every mode
  create --from <mode> ...   new LevelDataAsset next to the source one, optionally registered everywhere
  register <mode> ...        add an existing mode to the Training list / Tournament config / scenario tests

A mode is named by the middle part of its asset name: "<Arena> - <Mode> - LevelDataAsset".
Edit assets with the Unity editor closed: an open editor would not see the change until it reimports,
and could overwrite the lists from memory.
"""
import argparse
import re
import sys
import uuid
from pathlib import Path

PROJECT = Path(__file__).resolve().parents[4]
ASSETS = PROJECT / "Assets"
TESTS = ASSETS / "Tests" / "PlayMode" / "GameModesScenarioTests.cs"
SUFFIX = " - LevelDataAsset"

if sys.stdout.encoding and sys.stdout.encoding.lower() != "utf-8":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")


# ----------------------------------------------------------------------------- assets and guids

def read(path):
    return path.read_bytes().decode("utf-8")


def write_like(path, text, newline):
    path.write_bytes(text.replace("\r\n", "\n").replace("\n", newline).encode("utf-8"))


def write_asset(path, text):
    # The repository stores Unity YAML in LF (as Unity writes it), even where a working copy has CRLF.
    write_like(path, text, "\n")


def newline_of(text):
    return "\r\n" if "\r\n" in text else "\n"


def guid_of(path):
    return re.search(r"^guid: ([0-9a-f]{32})", read(Path(str(path) + ".meta")), re.M).group(1)


def script_guid(class_name):
    script = next(ASSETS.rglob(f"{class_name}.cs"))
    return guid_of(script)


def head(path, size=4096):
    with open(path, "rb") as f:
        return f.read(size).decode("utf-8", errors="replace")


def assets_using_script(class_name, pattern="*.asset"):
    """ScriptableObject assets of this class (m_Script is near the top of the file)."""
    guid = script_guid(class_name)
    return [p for p in ASSETS.rglob(pattern) if f"m_Script: {{fileID: 11500000, guid: {guid}" in head(p)]


def level_assets():
    return sorted(assets_using_script("LevelDataAsset"), key=lambda p: p.name)


def mode_name(level_path):
    base = level_path.stem[:-len(SUFFIX)] if level_path.stem.endswith(SUFFIX) else level_path.stem
    return base.split(" - ", 1)[1] if " - " in base else base


def arena_name(level_path):
    return level_path.stem.split(" - ")[0]


def find_level(name):
    matches = [p for p in level_assets() if name in (mode_name(p), p.stem)]
    if len(matches) != 1:
        known = ", ".join(sorted({mode_name(p) for p in level_assets()}))
        sys.exit(f"Mode '{name}' {'is ambiguous' if matches else 'not found'}. Known modes: {known}")
    return matches[0]


def blocks():
    """Prefabs usable in a LevelDataAsset, by short name: objectives, special actions, modifiers."""
    def collect(folder, suffix):
        return {p.stem[:-len(suffix)]: p for p in (ASSETS / folder).rglob(f"*{suffix}.prefab")}
    return {
        "objective": collect("Game/Gameplay/GameModes/ObjectiveManagement", "ObjectiveManager"),
        "special": collect("Game", "SpecialAction"),
        "modifier": collect("Game/Gameplay/GameModes/GameplayModifiers", "Modifier"),
    }


# Objectives that only work with one special action (they hook into its components).
# MostKills counts kills through the sniper's SniperBulletShooter: with another special action the game fails to load.
REQUIRED_SPECIAL = {"MostKills": "SniperRifle"}


def check_compatibility(objective_guid, special_guid):
    names = name_by_guid()
    objective, special = names.get(objective_guid), names.get(special_guid)
    required = REQUIRED_SPECIAL.get(objective)
    if required and special != required:
        sys.exit(f"The {objective} objective only works with the {required} special action (not {special}).")


def pick(kind, name):
    available = blocks()[kind]
    match = next((p for short, p in available.items() if short.lower() == name.lower()), None)
    if not match:
        sys.exit(f"Unknown {kind} '{name}'. Available: {', '.join(sorted(available))}")
    return match


# ----------------------------------------------------------------------------- level fields

def field_guids(text, field):
    match = re.search(rf"  <{field}>k__BackingField:(.*?)(?=\n  <|\Z)", text, re.S)
    return re.findall(r"(?:m_AssetGUID|guid): ([0-9a-f]{32})", match.group(1)) if match else []


def name_by_guid():
    names = {}
    for kind, items in blocks().items():
        for short, path in items.items():
            names[guid_of(path)] = short
    return names


def yaml_string(value):
    """Unity writes non-ASCII text as a double-quoted YAML string with \\x / \\u escapes."""
    if value.isascii() and re.fullmatch(r"[\w .,!?'()\-]+", value) and not value[0] in " -'":
        return value
    out = []
    for ch in value:
        code = ord(ch)
        if ch in '"\\':
            out.append("\\" + ch)
        elif 32 <= code < 127:
            out.append(ch)
        elif code < 0x100:
            out.append(f"\\x{code:02X}")
        else:
            out.append(f"\\u{code:04X}")
    return '"' + "".join(out) + '"'


def from_yaml_string(value):
    value = value.strip()
    if len(value) > 1 and value[0] == value[-1] == '"':
        return re.sub(r"\\(x[0-9A-Fa-f]{2}|u[0-9A-Fa-f]{4}|.)",
                      lambda m: chr(int(m.group(1)[1:], 16)) if m.group(1)[0] in "xu" and len(m.group(1)) > 1 else m.group(1),
                      value[1:-1])
    return value


def asset_reference(guid):
    return (f"  - m_AssetGUID: {guid}\n    m_SubObjectName: \n    m_SubObjectType: \n"
            f"    m_SubObjectGUID: \n    m_EditorAssetChanged: 0\n")


def set_reference(text, field, guid):
    pattern = rf"(  <{field}>k__BackingField:\n    m_AssetGUID: )[0-9a-f]{{32}}"
    if not re.search(pattern, text):
        sys.exit(f"Field {field} not found in the source asset.")
    return re.sub(pattern, lambda m: m.group(1) + guid, text)


def set_reference_list(text, field, guids):
    value = "\n" + "".join(asset_reference(g) for g in guids) if guids else " []\n"
    return re.sub(rf"  <{field}>k__BackingField:.*?(?=  <|\Z)", lambda _: f"  <{field}>k__BackingField:{value}", text,
                  count=1, flags=re.S)


# ----------------------------------------------------------------------------- registration

def lists():
    training = assets_using_script("LevelsListDataAsset")
    tournament = assets_using_script("TournamentConfigDataAsset")
    return {"training": training, "tournament": tournament}


def list_field(path):
    return "m_levelDataList" if "m_levelDataList:" in read(path) else "<LevelDataAssets>k__BackingField"


def register_in_list(path, guid):
    text = read(path).replace("\r\n", "\n")
    field = list_field(path)
    if f"guid: {guid}" in text:
        return False
    entry = f"  - {{fileID: 11400000, guid: {guid}, type: 2}}\n"
    match = re.search(rf"^  {re.escape(field)}:( \[\])?\n((?:  - .*\n)*)", text, re.M)
    if not match:
        sys.exit(f"List {field} not found in {path.name}.")
    if match.group(1):  # empty inline list
        text = text[:match.start()] + f"  {field}:\n{entry}" + text[match.end():]
    else:
        text = text[:match.end()] + entry + text[match.end():]
    write_asset(path, text)
    return True


def register_in_tests(mode):
    if not TESTS.is_file():
        return False
    raw = read(TESTS)
    nl = newline_of(raw)
    text = raw.replace("\r\n", "\n")
    match = re.search(r"(public static readonly string\[\] TrainingModes =\s*\{)(.*?)(\n(\s*)\};)", text, re.S)
    if not match:
        sys.exit("TrainingModes array not found in the scenario tests.")
    if f'"{mode}"' in match.group(2):
        return False
    indent = match.group(4) + "    "
    insert = f'\n{indent}"{mode}",'
    text = text[:match.end(2)] + insert + text[match.end(2):]
    write_like(TESTS, text, nl)
    return True


def register(level, training, tournament, tests):
    guid = guid_of(level)
    done = []
    found = lists()
    for key, wanted in (("training", training), ("tournament", tournament)):
        if not wanted:
            continue
        if len(found[key]) != 1:
            sys.exit(f"Expected one {key} list asset, found {len(found[key])}.")
        if register_in_list(found[key][0], guid):
            done.append(f"{key} list ({found[key][0].name})")
    if tests and register_in_tests(mode_name(level)):
        done.append(f"scenario tests ({TESTS.name})")
    return done


# ----------------------------------------------------------------------------- commands

def cmd_list(args):
    names = name_by_guid()
    registered = {key: set(re.findall(r"guid: ([0-9a-f]{32})", "".join(read(p) for p in paths)))
                  for key, paths in lists().items()}
    tests_text = read(TESTS) if TESTS.is_file() else ""
    print("Building blocks:")
    for kind, items in blocks().items():
        print(f"  {kind:10} {', '.join(sorted(items))}")
    print("\nModes (arena / mode: objective + special action + modifiers | display name | registered in):")
    for level in level_assets():
        text = read(level)
        guid = guid_of(level)
        parts = [", ".join(names.get(g, "?") for g in field_guids(text, f)) or "-"
                 for f in ("ObjectiveManagerPrefab", "SpecialActionPrefab", "AdditionalSystemsToInstantiate")]
        display = from_yaml_string(re.search(r"<NameKey>k__BackingField: (.*)", text).group(1))
        where = [k for k in ("training", "tournament") if guid in registered[k]]
        if f'"{mode_name(level)}"' in tests_text:
            where.append("tests")
        print(f"  {arena_name(level)} / {mode_name(level)}: {parts[0]} + {parts[1]} + {parts[2]} | {display} | "
              f"{', '.join(where) or 'nowhere'}")
    return 0


def cmd_create(args):
    source = find_level(args.source)
    arena = args.arena or arena_name(source)
    target = source.parent / f"{arena} - {args.mode}{SUFFIX}.asset"
    if target.exists():
        sys.exit(f"{target.name} already exists.")

    text = read(source).replace("\r\n", "\n")
    text = re.sub(r"^  m_Name: .*$", lambda _: f"  m_Name: {target.stem}", text, flags=re.M)
    if args.name:
        text = re.sub(r"^  <NameKey>k__BackingField: .*$", lambda _: f"  <NameKey>k__BackingField: {yaml_string(args.name)}",
                      text, flags=re.M)
    if args.objective:
        text = set_reference(text, "ObjectiveManagerPrefab", guid_of(pick("objective", args.objective)))
    if args.special:
        text = set_reference(text, "SpecialActionPrefab", guid_of(pick("special", args.special)))
    if args.modifiers is not None:
        names = [m for m in args.modifiers.split(",") if m and m.lower() != "none"]
        text = set_reference_list(text, "AdditionalSystemsToInstantiate", [guid_of(pick("modifier", m)) for m in names])
    check_compatibility(field_guids(text, "ObjectiveManagerPrefab")[0], field_guids(text, "SpecialActionPrefab")[0])

    meta = read(Path(str(source) + ".meta"))
    new_guid = uuid.uuid4().hex
    write_asset(target, text)
    write_asset(Path(str(target) + ".meta"), re.sub(r"^guid: [0-9a-f]{32}", f"guid: {new_guid}", meta, flags=re.M))
    print(f"Created {target.relative_to(PROJECT).as_posix()} (guid {new_guid}), from {source.name}.")

    done = register(target, args.training, args.tournament, args.training and not args.no_tests)
    for item in done:
        print(f"Registered in the {item}.")
    return 0


def cmd_register(args):
    level = find_level(args.mode)
    done = register(level, args.training, args.tournament, args.training and not args.no_tests)
    print("\n".join(f"Registered in the {item}." for item in done) or "Already registered everywhere asked.")
    return 0


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="command", required=True)
    sub.add_parser("list", help="building blocks and existing modes").set_defaults(func=cmd_list)

    p = sub.add_parser("create", help="new mode from an existing one")
    p.add_argument("--from", dest="source", required=True, help="existing mode to copy, e.g. 'Points Rush'")
    p.add_argument("--mode", required=True, help="new mode name, used in the asset name, e.g. 'Bomb Shooters'")
    p.add_argument("--name", help="display name shown in game (NameKey), e.g. 'Tir à la bombe'")
    p.add_argument("--arena", help="arena prefix of the asset name (default: the source's)")
    p.add_argument("--objective", help="objective manager, see `list`")
    p.add_argument("--special", help="special action, see `list`")
    p.add_argument("--modifiers", help="comma-separated modifiers, or 'none'")
    p.add_argument("--training", action="store_true", help="add to the Training list (and the scenario tests)")
    p.add_argument("--tournament", action="store_true", help="add to the Tournament levels")
    p.add_argument("--no-tests", action="store_true", help="don't add a scenario test case")
    p.set_defaults(func=cmd_create)

    p = sub.add_parser("register", help="add an existing mode to the lists")
    p.add_argument("mode")
    p.add_argument("--training", action="store_true")
    p.add_argument("--tournament", action="store_true")
    p.add_argument("--no-tests", action="store_true")
    p.set_defaults(func=cmd_register)

    args = parser.parse_args()
    sys.exit(args.func(args))


if __name__ == "__main__":
    main()
