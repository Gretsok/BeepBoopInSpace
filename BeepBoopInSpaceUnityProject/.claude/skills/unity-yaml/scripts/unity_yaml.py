#!/usr/bin/env python3
"""Unity YAML (scenes, prefabs, .asset, .meta) without the editor, for Beep Boop dans l'espace.
Python 3 standard library only.

  tree <asset> [--fields] [--rect]          hierarchy of a prefab or scene: objects, components, nested prefabs
  overrides <asset> [--instance NAME]       prefab instance overrides, with the object each one targets
  refs <asset path or guid>                 text assets referencing an asset, and which object/field does
  guid <guid or asset path>                 asset path of a guid, or guid of an asset
  meta <file>... [--guid G]                 create the .meta of new files (folder, cs, uxml, uss, tss, asset...)
  clean-overrides <asset>... [--dry-run]    remove prefab instance overrides whose target no longer exists
  uitk-prefab <prefab> --uxml <file> [--keep NAMES] [--panel-settings <asset>]
                                            turn a uGUI screen prefab into a UIDocument screen
  set-field <asset> <Object/Component> <field> (--value V | --object O | --asset A | --remove)
                                            set, replace or remove a serialized field of a component
  add-component <asset> <GameObject> <script.cs>
                                            add a MonoBehaviour to a GameObject

Edit with the Unity editor closed: an open editor would not see the change before reimporting, and could
save its own version over it. YAML is written in LF, as the repository stores it.
"""
import argparse
import json
import random
import re
import sys
import uuid
from pathlib import Path

PROJECT = Path(__file__).resolve().parents[4]
INDEX_FILE = PROJECT / "Library" / "claude-guid-index.json"
DEFAULT_PANEL_SETTINGS = "Assets/Game/UITools/Screens/UIPanelSettings.asset"
MASK = 0x7FFFFFFFFFFFFFFF

if sys.stdout.encoding and sys.stdout.encoding.lower() != "utf-8":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")

DOC_RE = re.compile(r"^--- !u!(\d+) &(-?\d+)( stripped)?[^\n]*\n(.*?)(?=^--- |\Z)", re.S | re.M)
MOD_RE = re.compile(r"(    - target: \{fileID: (-?\d+), guid: (\w+), type: \d+\}\n(?:      .*\n)+)")
REF_RE = re.compile(r"\{fileID: (-?\d+)(?:, guid: (\w+), type: (\d+))?\}")

CLASS_NAMES = {
    "1": "GameObject", "4": "Transform", "20": "Camera", "23": "MeshRenderer", "33": "MeshFilter",
    "54": "Rigidbody", "64": "MeshCollider", "65": "BoxCollider", "81": "AudioListener", "82": "AudioSource",
    "95": "Animator", "96": "TrailRenderer", "108": "Light", "114": "MonoBehaviour", "120": "LineRenderer",
    "135": "SphereCollider", "136": "CapsuleCollider", "137": "SkinnedMeshRenderer", "195": "NavMeshAgent",
    "198": "ParticleSystem", "199": "ParticleSystemRenderer", "212": "SpriteRenderer", "222": "CanvasRenderer",
    "223": "Canvas", "224": "RectTransform", "225": "CanvasGroup", "1001": "PrefabInstance",
}
# Scripts compiled into the engine (guid 0000000000000000e000000000000000), by fileID.
BUILTIN_SCRIPTS = {"19101": "PanelSettings", "19102": "UIDocument"}
BOILERPLATE = ("m_ObjectHideFlags", "m_CorrespondingSourceObject", "m_PrefabInstance", "m_PrefabAsset",
               "m_GameObject", "m_Enabled", "m_EditorHideFlags", "m_Script", "m_Name", "m_EditorClassIdentifier",
               "serializedVersion")
TEXT_EXTENSIONS = {".unity", ".prefab", ".asset", ".mat", ".controller", ".overrideController", ".anim", ".uxml",
                   ".uss", ".tss", ".playable", ".mask", ".physicMaterial", ".physicsMaterial2D", ".lighting",
                   ".spriteatlas", ".spriteatlasv2", ".shadergraph", ".shadersubgraph", ".vfx", ".signal",
                   ".mixer", ".guiskin", ".fontsettings", ".renderTexture", ".terrainlayer", ".brush", ".flare"}


# ----------------------------------------------------------------------------- files and guids

def project_path(value):
    """Accepts project-relative, absolute and Git Bash (/c/...) paths."""
    match = re.match(r"^/([a-zA-Z])/(.*)$", str(value))
    path = Path(f"{match.group(1)}:/{match.group(2)}") if match else Path(value)
    return path if path.is_absolute() else PROJECT / path


def rel(path):
    try:
        return Path(path).resolve().relative_to(PROJECT).as_posix()
    except ValueError:
        return str(path)


def read_text(path):
    return Path(path).read_bytes().decode("utf-8").replace("\r\n", "\n")


def write_yaml(path, text):
    Path(path).write_bytes(text.replace("\r\n", "\n").encode("utf-8"))


def scan_metas(roots):
    index = {}
    for root in roots:
        if not root.is_dir():
            continue
        for meta in root.rglob("*.meta"):
            try:
                with open(meta, "rb") as f:
                    head = f.read(200).decode("utf-8", errors="ignore")
            except OSError:
                continue
            match = re.search(r"^guid: ([0-9a-f]{32})", head, re.M)
            if match:
                index[match.group(1)] = rel(str(meta)[:-5])
    return index


_index = None


def guid_index(refresh=False):
    """guid -> project path. Assets are scanned each time; packages are cached in Library (rebuilt with it)."""
    global _index
    if _index is not None and not refresh:
        return _index
    packages = None
    if INDEX_FILE.is_file() and not refresh:
        try:
            packages = json.loads(INDEX_FILE.read_text(encoding="utf-8"))
        except ValueError:
            packages = None
    if packages is None:
        packages = scan_metas([PROJECT / "Library" / "PackageCache", PROJECT / "Packages"])
        INDEX_FILE.parent.mkdir(parents=True, exist_ok=True)
        INDEX_FILE.write_text(json.dumps(packages), encoding="utf-8")
    _index = {**packages, **scan_metas([PROJECT / "Assets"])}
    return _index


def path_of(guid):
    if guid == "0000000000000000e000000000000000":
        return "<engine script>"
    if guid == "0000000000000000f000000000000000":
        return "<builtin resource>"
    return guid_index().get(guid) or guid_index(refresh=True).get(guid)


def guid_of(path):
    meta = Path(str(project_path(path)) + ".meta")
    match = re.search(r"^guid: ([0-9a-f]{32})", read_text(meta), re.M) if meta.is_file() else None
    return match.group(1) if match else None


def short(path):
    return Path(path).name if path and not path.startswith("<") else path


# ----------------------------------------------------------------------------- YAML model

class Doc:
    def __init__(self, cls, fid, stripped, body):
        self.cls, self.fid, self.stripped, self.body = cls, fid, stripped, body

    def field(self, name):
        match = re.search(r"^  " + re.escape(name) + r": ?(.*)$", self.body, re.M)
        return match.group(1).strip() if match else None

    def ref(self, name):
        value = self.field(name)
        match = REF_RE.search(value) if value else None
        return match.group(1) if match else None

    def text(self):
        return f"--- !u!{self.cls} &{self.fid}{' stripped' if self.stripped else ''}\n{self.body}"


class Model:
    _cache = {}

    def __init__(self, path):
        self.path = Path(path)
        text = read_text(path)
        first = text.find("--- ")
        self.header = text[:first] if first >= 0 else text
        self.docs = [Doc(m.group(1), m.group(2), bool(m.group(3)), m.group(4)) for m in DOC_RE.finditer(text)]
        self.by_id = {d.fid: d for d in self.docs}
        self.index()

    @classmethod
    def of_guid(cls, guid):
        path = path_of(guid)
        if not path or path.startswith("<"):
            return None
        if guid not in cls._cache:
            full = PROJECT / path
            cls._cache[guid] = cls(full) if full.suffix in (".prefab", ".unity") and full.is_file() else None
        return cls._cache[guid]

    def index(self):
        self.go_of = {}          # component -> GameObject
        self.transform_of = {}   # GameObject -> its Transform
        self.father = {}         # transform -> parent transform ("0" at the root)
        for d in self.docs:
            if d.stripped:
                continue
            go = d.ref("m_GameObject")
            if go and go != "0":
                self.go_of[d.fid] = go
                if d.cls in ("4", "224"):
                    self.transform_of[go] = d.fid
                    self.father[d.fid] = d.ref("m_Father") or "0"

    def children(self, transform):
        body = self.by_id[transform].body if transform in self.by_id else ""
        listed = re.findall(r"^  - \{fileID: (-?\d+)\}", body.split("m_Children:")[1].split("\n  m_")[0], re.M) \
            if "m_Children:" in body else []
        added = [t for t, f in self.father.items() if f == transform and t not in listed]
        return listed + added

    def instance_of(self, doc):
        return doc.ref("m_PrefabInstance") if doc.stripped else None

    def instances(self):
        return [d for d in self.docs if d.cls == "1001"]

    @staticmethod
    def instance_parent(instance_doc):
        match = re.search(r"m_TransformParent: \{fileID: (-?\d+)", instance_doc.body)
        return match.group(1) if match else "0"

    def go_name(self, go):
        doc = self.by_id.get(go)
        return doc.field("m_Name") if doc else None

    def script_name(self, doc):
        value = doc.field("m_Script") or ""
        match = REF_RE.search(value)
        if not match:
            return None
        fid, guid = match.group(1), match.group(2)
        if guid == "0000000000000000e000000000000000":
            return BUILTIN_SCRIPTS.get(fid, f"engine script {fid}")
        path = path_of(guid)
        return Path(path).stem if path else f"missing script {guid}"

    def type_name(self, doc):
        if doc.cls == "114":
            return self.script_name(doc) or "MonoBehaviour"
        if doc.cls == "1001":
            return "PrefabInstance"
        return CLASS_NAMES.get(doc.cls, f"class {doc.cls}")

    def instance_name(self, instance):
        doc = self.by_id[instance]
        mods = re.findall(r"propertyPath: m_Name\n\s*value: (.*)\n", doc.body)
        source = REF_RE.search(doc.field("m_SourcePrefab") or "")
        source_path = path_of(source.group(2)) if source else None
        return mods[0] if mods else (Path(source_path).stem if source_path else "?"), source_path

    def describe(self, fid):
        """Readable name of an object of this file, including objects of nested prefabs."""
        doc = self.by_id.get(fid)
        if doc is None:
            nested = self.resolve_nested(fid)
            return nested
        if doc.stripped:
            instance = self.instance_of(doc)
            name, _ = self.instance_name(instance) if instance in self.by_id else ("?", None)
            source = self.source_model(instance)
            inner = source.describe(REF_RE.search(doc.field("m_CorrespondingSourceObject")).group(1)) if source else None
            return f"{name}:{inner}" if inner else f"{name}:{self.type_name(doc)}"
        if doc.cls == "1":
            return doc.field("m_Name")
        if doc.cls == "1001":
            return f"{self.instance_name(fid)[0]} (prefab instance)"
        go = self.go_of.get(fid)
        return f"{self.go_name(go) if go else '?'}/{self.type_name(doc)}"

    def source_model(self, instance):
        doc = self.by_id.get(instance)
        source = REF_RE.search(doc.field("m_SourcePrefab") or "") if doc else None
        return Model.of_guid(source.group(2)) if source else None

    def resolve_nested(self, fid, depth=0):
        """Objects of nested prefabs have implicit ids: (instance id ^ id in the source prefab) & 0x7FFF..."""
        if depth > 4:
            return None
        target = int(fid)
        for instance in self.instances():
            source = self.source_model(instance.fid)
            if not source:
                continue
            inner = (int(instance.fid) ^ target) & MASK
            # The mask drops the sign bit: the source id may be the negative one.
            for candidate in (str(inner), str(inner - (1 << 63))):
                if candidate in source.by_id or source.resolve_nested(candidate, depth + 1):
                    name = self.instance_name(instance.fid)[0]
                    return f"{name}:{source.describe(candidate)}"
        return None

    def contains(self, fid):
        return fid in self.by_id or self.resolve_nested(fid) is not None

    def text(self):
        return self.header + "".join(d.text() for d in self.docs)

    def save(self):
        write_yaml(self.path, self.text())


def resolve_value(model, value):
    """Replaces object references in a YAML value with the object or asset they point to."""
    def replace(match):
        fid, guid = match.group(1), match.group(2)
        if fid == "0":
            return "None"
        if not guid:
            return f"→{model.describe(fid) or 'missing ' + fid}"
        path = path_of(guid) or f"missing asset {guid}"
        return f"→{short(path)}" + ("" if fid in ("11400000", "100100000", "2100000", "2800000", "102900000",
                                                  "8300000", "4300000", "9197481963319205126",
                                                  "7433441132597879392", "-4733365628477956816", "12800000")
                                    else f"#{fid}")
    return REF_RE.sub(replace, value)


# ----------------------------------------------------------------------------- tree

def component_summary(model, doc):
    kind = model.type_name(doc)
    details = []
    if doc.field("m_Enabled") == "0":
        details.append("disabled")
    if kind in ("TextMeshProUGUI", "TextMeshPro"):
        text = re.search(r"^  m_text: (.*?)(?=^  m_\w+:)", doc.body, re.M | re.S)
        if text:
            details.append(repr(" ".join(text.group(1).split())[:60]))
    elif kind in ("Image", "RawImage", "SpriteRenderer"):
        sprite = doc.field("m_Sprite") or doc.field("m_Texture")
        if sprite and "guid" in sprite:
            details.append(resolve_value(model, sprite))
    elif kind == "UIDocument":
        details.append(resolve_value(model, doc.field("sourceAsset") or ""))
    elif kind == "AudioSource":
        details.append(resolve_value(model, doc.field("m_audioClip") or ""))
    return kind + (f" ({', '.join(details)})" if details else "")


def print_fields(model, doc, indent):
    lines = doc.body.split("\n")[1:]
    skip = False
    for line in lines:
        if not line.strip():
            continue
        key = re.match(r"^  ([^ ][^:]*):", line)
        if key:
            skip = key.group(1) in BOILERPLATE
        if skip:
            continue
        resolved = resolve_value(model, line[2:])
        print(f"{indent}  {resolved[:200]}")


def cmd_tree(args):
    model = Model(project_path(args.asset))
    roots = [t for t, f in model.father.items() if f == "0"]
    scene_roots = next((d for d in model.docs if d.cls == "1660057539"), None)
    if scene_roots:
        ordered = re.findall(r"- \{fileID: (-?\d+)\}", scene_roots.body)
        roots = [r for r in ordered if r in model.by_id] + [r for r in roots if r not in ordered]
    root_instances = [d for d in model.instances() if model.instance_parent(d) == "0"]

    def walk(transform, depth):
        pad = "  " * depth
        doc = model.by_id.get(transform)
        if doc is None:
            print(f"{pad}- <missing transform {transform}>")
            return
        if doc.stripped:
            instance = model.instance_of(doc)
            name, source = model.instance_name(instance)
            inactive = re.search(r"propertyPath: m_IsActive\n\s*value: 0\n", model.by_id[instance].body)
            print(f"{pad}- {name} [prefab {short(source)}{', inactive' if inactive else ''}] (instance {instance})")
        else:
            go = doc.ref("m_GameObject")
            go_doc = model.by_id.get(go)
            inactive = go_doc and go_doc.field("m_IsActive") == "0"
            print(f"{pad}- {model.go_name(go)}{' [inactive]' if inactive else ''} (GameObject {go})")
            for comp in re.findall(r"- component: \{fileID: (-?\d+)\}", go_doc.body if go_doc else ""):
                comp_doc = model.by_id.get(comp)
                if comp_doc is None:
                    print(f"{pad}    · <missing component {comp}>")
                    continue
                if comp_doc.cls in ("4", "224") and not (args.rect and comp_doc.cls == "224"):
                    continue
                if comp_doc.cls == "224":
                    print(f"{pad}    · RectTransform anchors {comp_doc.field('m_AnchorMin')}..{comp_doc.field('m_AnchorMax')} "
                          f"pos {comp_doc.field('m_AnchoredPosition')} size {comp_doc.field('m_SizeDelta')} pivot {comp_doc.field('m_Pivot')}")
                    continue
                print(f"{pad}    · {component_summary(model, comp_doc)} ({comp})")
                if args.fields:
                    print_fields(model, comp_doc, pad + "    ")
        for child in model.children(transform):
            walk(child, depth + 1)

    for root in roots:
        walk(root, 0)
    for instance in root_instances:
        name, source = model.instance_name(instance.fid)
        print(f"- {name} [prefab {short(source)}] (instance {instance.fid})")
    # ScriptableObjects (.asset files): no GameObject.
    other = [d for d in model.docs if not d.stripped and d.cls == "114" and d.fid not in model.go_of]
    for doc in other:
        print(f"* {model.type_name(doc)} {doc.field('m_Name') or ''} ({doc.fid})")
        if args.fields:
            print_fields(model, doc, "  ")
    return 0


# ----------------------------------------------------------------------------- overrides

def modifications(model, instance_doc):
    source = model.source_model(instance_doc.fid)
    for block, target, guid in MOD_RE.findall(instance_doc.body):
        prop = re.search(r"propertyPath: (.*)\n", block).group(1)
        value = re.search(r"value: (.*)\n", block)
        obj = re.search(r"objectReference: (.*)\n", block)
        described = source.describe(target) if source else None
        missing = source is not None and not source.contains(target)
        yield block, target, described, missing, prop, value.group(1) if value else "", obj.group(1) if obj else ""


def cmd_overrides(args):
    model = Model(project_path(args.asset))
    for instance in model.instances():
        name, source_path = model.instance_name(instance.fid)
        if args.instance and args.instance.lower() not in name.lower():
            continue
        parent = model.instance_parent(instance)
        print(f"== {name} [prefab {source_path}] (instance {instance.fid}, parent {model.describe(parent) if parent != '0' else 'scene root'})")
        for _, target, described, missing, prop, value, obj in modifications(model, instance):
            shown = resolve_value(model, obj) if obj and obj != "{fileID: 0}" else value
            label = f"[MISSING {target}]" if missing else (described or target)
            print(f"   {label}  {prop} = {shown[:100]}")
        for key in ("m_RemovedComponents", "m_RemovedGameObjects", "m_AddedGameObjects", "m_AddedComponents"):
            block = re.search(key + r":(.*?)(?=\n    m_|\n  m_)", instance.body, re.S)
            if block and block.group(1).strip() not in ("[]", ""):
                print(f"   {key}: {' '.join(block.group(1).split())[:300]}")
    return 0


def cmd_clean_overrides(args):
    total = 0
    for asset in args.assets:
        model = Model(project_path(asset))
        removed = 0
        for instance in model.instances():
            name, _ = model.instance_name(instance.fid)
            for block, target, _, missing, prop, value, _ in list(modifications(model, instance)):
                if missing:
                    print(f"{rel(model.path)}: {name}: [MISSING {target}] {prop} = {value[:60]}")
                    instance.body = instance.body.replace(block, "", 1)
                    removed += 1
        if removed and not args.dry_run:
            model.save()
        print(f"{rel(model.path)}: {removed} override(s) on missing objects {'found' if args.dry_run else 'removed'}.")
        total += removed
    return 0


# ----------------------------------------------------------------------------- refs, guid, meta

def cmd_refs(args):
    target = args.target if re.fullmatch(r"[0-9a-f]{32}", args.target) else guid_of(args.target)
    if not target:
        print(f"No .meta for {args.target}.")
        return 1
    print(f"guid {target} = {path_of(target)}")
    found = 0
    for root in (PROJECT / "Assets", PROJECT / "ProjectSettings"):
        for path in root.rglob("*"):
            if path.suffix not in TEXT_EXTENSIONS and not (root.name == "ProjectSettings" and path.suffix == ".asset"):
                continue
            try:
                raw = path.read_bytes()
            except OSError:
                continue
            if target.encode() not in raw:
                continue
            found += 1
            text = raw.decode("utf-8", errors="replace").replace("\r\n", "\n")
            if path.suffix in (".unity", ".prefab") or text.startswith("%YAML"):
                model = Model(path)
                for doc in model.docs:
                    for line in doc.body.split("\n"):
                        if target in line:
                            key = re.match(r"^\s*(?:- )?([^:{]+):", line)
                            owner = model.describe(doc.fid) or model.type_name(doc)
                            print(f"{rel(path)}: {owner}" + (f" ({key.group(1).strip()})" if key else ""))
            else:
                print(f"{rel(path)}")
    if not found:
        print("No reference in Assets or ProjectSettings (Addressables groups and code by name are not checked).")
    return 0


def cmd_guid(args):
    if re.fullmatch(r"[0-9a-f]{32}", args.value):
        path = path_of(args.value)
        print(path or "Unknown guid.")
        return 0 if path else 1
    guid = guid_of(args.value)
    print(guid or f"No .meta for {args.value}.")
    return 0 if guid else 1


META_TEMPLATES = {
    # Unity 6.6 writes scripts' metas without importer settings.
    ".cs": "fileFormatVersion: 2\nguid: {guid}",
    ".uxml": "fileFormatVersion: 2\nguid: {guid}\nScriptedImporter:\n  internalIDToNameTable: []\n  externalObjects: {{}}\n"
             "  serializedVersion: 2\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
             "  script: {{fileID: 13804, guid: 0000000000000000e000000000000000, type: 0}}\n",
    ".uss": "fileFormatVersion: 2\nguid: {guid}\nScriptedImporter:\n  internalIDToNameTable: []\n  externalObjects: {{}}\n"
            "  serializedVersion: 2\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
            "  script: {{fileID: 12385, guid: 0000000000000000e000000000000000, type: 0}}\n  disableValidation: 0\n",
    ".tss": "fileFormatVersion: 2\nguid: {guid}\nScriptedImporter:\n  internalIDToNameTable: []\n  externalObjects: {{}}\n"
            "  serializedVersion: 2\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
            "  script: {{fileID: 12388, guid: 0000000000000000e000000000000000, type: 0}}\n  disableValidation: 0\n",
    ".prefab": "fileFormatVersion: 2\nguid: {guid}\nPrefabImporter:\n  externalObjects: {{}}\n  userData: \n"
               "  assetBundleName: \n  assetBundleVariant: \n",
    ".unity": "fileFormatVersion: 2\nguid: {guid}\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n"
              "  assetBundleName: \n  assetBundleVariant: \n",
    "folder": "fileFormatVersion: 2\nguid: {guid}\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {{}}\n"
              "  userData: \n  assetBundleName: \n  assetBundleVariant: \n",
}
NATIVE_MAIN_OBJECTS = {".asset": 11400000, ".mat": 2100000, ".renderTexture": 8400000, ".mixer": 24100000,
                       ".controller": 9100000, ".anim": 7400000, ".physicMaterial": 13400000}
TEXT_ASSETS = (".txt", ".json", ".md", ".xml", ".bytes", ".csv", ".yaml", ".html")


def cmd_meta(args):
    if args.guid and len(args.files) > 1:
        print("--guid needs a single file.")
        return 1
    for file in args.files:
        path = project_path(file)
        meta = Path(str(path) + ".meta")
        if meta.exists():
            print(f"{rel(meta)} already exists (guid {guid_of(path)}).")
            continue
        if not path.exists():
            print(f"{rel(path)} doesn't exist: create the file first.")
            return 1
        guid = args.guid or uuid.uuid4().hex
        key = "folder" if path.is_dir() else path.suffix
        if key in META_TEMPLATES:
            text = META_TEMPLATES[key].format(guid=guid)
        elif key in NATIVE_MAIN_OBJECTS:
            text = (f"fileFormatVersion: 2\nguid: {guid}\nNativeFormatImporter:\n  externalObjects: {{}}\n"
                    f"  mainObjectFileID: {NATIVE_MAIN_OBJECTS[key]}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n")
        elif key in TEXT_ASSETS:
            text = (f"fileFormatVersion: 2\nguid: {guid}\nTextScriptImporter:\n  externalObjects: {{}}\n  userData: \n"
                    "  assetBundleName: \n  assetBundleVariant: \n")
        else:
            print(f"{rel(path)}: no template for '{key}' (textures, models and audio have per-file import settings): "
                  "let Unity create its .meta, or copy the .meta of a similar asset and change its guid.")
            return 1
        write_yaml(meta, text)
        print(f"{rel(meta)}: guid {guid}")
    return 0


# ----------------------------------------------------------------------------- uitk-prefab

def is_ugui(model, doc):
    if doc.cls in ("222", "223", "225"):  # CanvasRenderer, Canvas, CanvasGroup
        return True
    if doc.cls != "114":
        return False
    script = REF_RE.search(doc.field("m_Script") or "")
    path = path_of(script.group(2)) if script and script.group(2) else ""
    return bool(path) and ("com.unity.ugui" in path or "TextMesh Pro" in path)


def cmd_uitk_prefab(args):
    model = Model(project_path(args.prefab))
    uxml_guid = guid_of(args.uxml)
    panel_guid = guid_of(args.panel_settings)
    if not uxml_guid or not panel_guid:
        print(f"Missing .meta for {args.uxml if not uxml_guid else args.panel_settings}: run `meta` on new files first.")
        return 1
    if any(model.type_name(d) == "UIDocument" for d in model.docs):
        print("The prefab already has a UIDocument.")
        return 1
    root_t = next(t for t, f in model.father.items() if f == "0")
    root_go = model.by_id[root_t].ref("m_GameObject")
    keep_names = {name.strip() for name in (args.keep or "").split(",") if name.strip()}

    removed, kept_tops = set(), []

    def subtree(transform):
        result = [transform]
        for child in model.children(transform):
            result += subtree(child)
        return result

    def drop(transform):
        for t in subtree(transform):
            doc = model.by_id.get(t)
            if doc is None:
                continue
            if doc.stripped:
                instance = model.instance_of(doc)
                removed.add(instance)
                removed.update(d.fid for d in model.docs if d.stripped and model.instance_of(d) == instance)
                continue
            go = doc.ref("m_GameObject")
            removed.add(go)
            removed.update(re.findall(r"- component: \{fileID: (-?\d+)\}", model.by_id[go].body))

    def visit(transform):
        for child in model.children(transform):
            doc = model.by_id.get(child)
            if doc is None:
                continue
            if doc.stripped:
                if model.instance_name(model.instance_of(doc))[0] in keep_names:
                    kept_tops.append(child)
                else:
                    drop(child)
                continue
            go = doc.ref("m_GameObject")
            if model.go_name(go) in keep_names:
                kept_tops.append(child)
                continue
            # Kept descendants move up to the root; this object goes.
            visit(child)
            removed.add(go)
            removed.update(re.findall(r"- component: \{fileID: (-?\d+)\}", model.by_id[go].body))

    visit(root_t)
    kept_tops = [t for t in kept_tops if t not in removed]

    # uGUI components go, on the root and on everything kept.
    for doc in model.docs:
        if doc.fid not in removed and not doc.stripped and is_ugui(model, doc):
            removed.add(doc.fid)

    for doc in model.docs:
        if doc.fid in removed or doc.stripped:
            continue
        if doc.cls == "1":
            comps = [c for c in re.findall(r"- component: \{fileID: (-?\d+)\}", doc.body) if c not in removed]
            doc.body = re.sub(r"  m_Component:\n(?:  - component: .*\n)*",
                              lambda _: "  m_Component:\n" + "".join(f"  - component: {{fileID: {c}}}\n" for c in comps), doc.body)
        if doc.fid in kept_tops:
            doc.body = re.sub(r"m_Father: \{fileID: -?\d+\}", lambda _: f"m_Father: {{fileID: {root_t}}}", doc.body)
        if doc.cls == "1001" and doc.fid in {model.instance_of(model.by_id[t]) for t in kept_tops if model.by_id[t].stripped}:
            doc.body = re.sub(r"m_TransformParent: \{fileID: -?\d+\}", lambda _: f"m_TransformParent: {{fileID: {root_t}}}", doc.body)

    root_doc = model.by_id[root_t]
    root_doc.body = root_doc.body.replace("m_LocalScale: {x: 0, y: 0, z: 0}", "m_LocalScale: {x: 1, y: 1, z: 1}")
    children = "".join(f"  - {{fileID: {t}}}\n" for t in kept_tops)
    root_doc.body = re.sub(r"  m_Children:( \[\])?\n(?:  - \{fileID: .*\}\n)*",
                           lambda _: ("  m_Children:\n" + children) if kept_tops else "  m_Children: []\n", root_doc.body)

    used = {d.fid for d in model.docs}
    uidoc_id = str(random.randint(10 ** 17, 9 * 10 ** 18))
    while uidoc_id in used:
        uidoc_id = str(random.randint(10 ** 17, 9 * 10 ** 18))
    uidoc = Doc("114", uidoc_id, False,
                "MonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n"
                "  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n"
                f"  m_GameObject: {{fileID: {root_go}}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n"
                "  m_Script: {fileID: 19102, guid: 0000000000000000e000000000000000, type: 0}\n"
                "  m_Name: \n  m_EditorClassIdentifier: \n"
                f"  m_PanelSettings: {{fileID: 11400000, guid: {panel_guid}, type: 2}}\n  m_ParentUI: {{fileID: 0}}\n"
                f"  sourceAsset: {{fileID: 9197481963319205126, guid: {uxml_guid}, type: 3}}\n  m_SortingOrder: 0\n")
    go_doc = model.by_id[root_go]
    go_doc.body = re.sub(r"(  m_Component:\n(?:  - component: .*\n)*)",
                         lambda m: m.group(1) + f"  - component: {{fileID: {uidoc_id}}}\n", go_doc.body)

    model.docs = [d for d in model.docs if d.fid not in removed] + [uidoc]
    model.by_id = {d.fid: d for d in model.docs}
    kept_names = [model.instance_name(model.instance_of(model.by_id[t]))[0] if model.by_id[t].stripped
                  else model.go_name(model.by_id[t].ref("m_GameObject")) for t in kept_tops]
    print(f"{rel(model.path)}: removed {len(removed)} object(s)/component(s), kept {kept_names or 'nothing'} "
          f"under the root, added UIDocument {uidoc_id}.")
    missing_keep = keep_names - set(kept_names)
    if missing_keep:
        print(f"Not found, so not kept: {', '.join(sorted(missing_keep))}")

    dangling = []
    for doc in model.docs:
        for line in doc.body.split("\n"):
            for match in REF_RE.finditer(line):
                if not match.group(2) and match.group(1) in removed:
                    key = re.match(r"^\s*(?:- )?([^:{]+):", line)
                    dangling.append(f"{model.describe(doc.fid)}: {key.group(1).strip() if key else line.strip()} → removed {match.group(1)}")
    if dangling:
        print("References to removed objects, to update (screen fields, UnityEvents):")
        for line in dangling:
            print(f"  {line}")
    if args.dry_run:
        print("Dry run: nothing written.")
        return 0
    model.save()
    print("Next: update those fields, then `clean-overrides` on the scenes and prefabs that use this prefab "
          f"(`refs {rel(model.path)}`).")
    return 0


# ----------------------------------------------------------------------------- set-field, add-component

# Main object of an asset referenced from a field, by extension: (fileID, type).
ASSET_REFERENCES = {".asset": ("11400000", "2"), ".uxml": ("9197481963319205126", "3"),
                    ".uss": ("7433441132597879392", "3"), ".tss": ("-4733365628477956816", "3")}


def new_file_id(model):
    used = {d.fid for d in model.docs}
    fid = str(random.randint(10 ** 17, 9 * 10 ** 18))
    while fid in used:
        fid = str(random.randint(10 ** 17, 9 * 10 ** 18))
    return fid


def find_objects(model, name):
    """Components named as `tree` shows them ("Object/Component"), or GameObjects by name."""
    if "/" in name:
        return [d for d in model.docs if not d.stripped and d.cls not in ("1", "1001") and d.fid in model.go_of
                and model.describe(d.fid) == name]
    return [d for d in model.docs if not d.stripped and d.cls == "1" and d.field("m_Name") == name]


def find_one(model, name):
    found = find_objects(model, name)
    if len(found) != 1:
        raise SystemExit(f"{rel(model.path)}: {len(found)} object(s) named '{name}' (`tree` shows the names: "
                         "\"Object/Component\" for a component).")
    return found[0]


def cmd_set_field(args):
    model = Model(project_path(args.asset))
    doc = find_one(model, args.component)
    if args.remove:
        line = None
    elif args.value is not None:
        line = f"  {args.field}: {args.value}\n"
    elif args.object:
        line = f"  {args.field}: {{fileID: {find_one(model, args.object).fid}}}\n"
    elif args.asset_ref:
        path = project_path(args.asset_ref)
        guid, ref = guid_of(path), ASSET_REFERENCES.get(path.suffix)
        if not guid or not ref:
            print(f"{args.asset_ref}: {'no .meta' if not guid else 'no main object known for ' + path.suffix} "
                  f"(known: {', '.join(ASSET_REFERENCES)}); pass the reference with --value.")
            return 1
        line = f"  {args.field}: {{fileID: {ref[0]}, guid: {guid}, type: {ref[1]}}}\n"
    else:
        print("Give --value, --object, --asset or --remove.")
        return 1

    # A field and its continuation lines (list items, nested mappings).
    block = re.compile(r"^  " + re.escape(args.field) + r":.*\n(?:(?:  - |   ).*\n)*", re.M)
    if block.search(doc.body):
        doc.body = block.sub(lambda _: line or "", doc.body, count=1)
        action = "removed" if line is None else "replaced"
    elif line is None:
        print(f"{model.describe(doc.fid)} has no field {args.field}.")
        return 1
    else:
        # New fields go after the header, whatever m_EditorClassIdentifier holds (often empty, not always).
        header = re.search(r"^  m_EditorClassIdentifier:.*\n", doc.body, re.M)
        if not header:
            print(f"{model.describe(doc.fid)}: no m_EditorClassIdentifier line to add the field after.")
            return 1
        doc.body = doc.body[:header.end()] + line + doc.body[header.end():]
        action = "added"
    model.save()
    print(f"{rel(model.path)}: {model.describe(doc.fid)} {args.field} {action}"
          + ("" if line is None else f": {resolve_value(model, line.split(': ', 1)[1].strip())}"))
    return 0


def cmd_add_component(args):
    model = Model(project_path(args.asset))
    go = find_one(model, args.object)
    if go.cls != "1":
        print(f"'{args.object}' is a component: give the GameObject's name.")
        return 1
    guid = guid_of(args.script)
    if not guid:
        print(f"{args.script}: no .meta (create it with `meta`).")
        return 1
    fid = new_file_id(model)
    model.docs.append(Doc("114", fid, False,
                          "MonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n"
                          "  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n"
                          f"  m_GameObject: {{fileID: {go.fid}}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n"
                          f"  m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}\n"
                          "  m_Name: \n  m_EditorClassIdentifier: \n"))
    go.body = re.sub(r"(  m_Component:\n(?:  - component: .*\n)*)",
                     lambda m: m.group(1) + f"  - component: {{fileID: {fid}}}\n", go.body, count=1)
    model.by_id[fid] = model.docs[-1]
    model.save()
    print(f"{rel(model.path)}: {Path(args.script).stem} {fid} added to {args.object}. Its fields take their default "
          "values: set the others with `set-field`.")
    return 0


# ----------------------------------------------------------------------------- main

def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="command", required=True)

    p = sub.add_parser("tree", help="hierarchy of a prefab or scene")
    p.add_argument("asset")
    p.add_argument("--fields", action="store_true", help="serialized fields of every component, references resolved")
    p.add_argument("--rect", action="store_true", help="RectTransform anchors, position, size")
    p.set_defaults(func=cmd_tree)

    p = sub.add_parser("overrides", help="prefab instance overrides and their targets")
    p.add_argument("asset")
    p.add_argument("--instance", help="only instances whose name contains this")
    p.set_defaults(func=cmd_overrides)

    p = sub.add_parser("refs", help="text assets referencing an asset")
    p.add_argument("target", help="asset path or guid")
    p.set_defaults(func=cmd_refs)

    p = sub.add_parser("guid", help="guid <-> asset path")
    p.add_argument("value")
    p.set_defaults(func=cmd_guid)

    p = sub.add_parser("meta", help="create the .meta of new files")
    p.add_argument("files", nargs="+")
    p.add_argument("--guid", help="guid to use (one file only)")
    p.set_defaults(func=cmd_meta)

    p = sub.add_parser("clean-overrides", help="remove overrides whose target no longer exists")
    p.add_argument("assets", nargs="+")
    p.add_argument("--dry-run", action="store_true")
    p.set_defaults(func=cmd_clean_overrides)

    p = sub.add_parser("uitk-prefab", help="turn a uGUI screen prefab into a UIDocument screen")
    p.add_argument("prefab")
    p.add_argument("--uxml", required=True)
    p.add_argument("--keep", help="comma-separated names of child objects or nested prefabs to keep (moved under the root)")
    p.add_argument("--panel-settings", default=DEFAULT_PANEL_SETTINGS)
    p.add_argument("--dry-run", action="store_true")
    p.set_defaults(func=cmd_uitk_prefab)

    p = sub.add_parser("set-field", help="set, replace or remove a serialized field of a component")
    p.add_argument("asset")
    p.add_argument("component", help="\"Object/Component\", as tree shows it")
    p.add_argument("field", help="serialized name: m_xxx, or <Xxx>k__BackingField for [field: SerializeField]")
    value = p.add_mutually_exclusive_group(required=True)
    value.add_argument("--value", help="raw YAML value: 2, \"text\", {fileID: 0}...")
    value.add_argument("--object", help="reference to an object of the same file (\"Object/Component\" or GameObject name)")
    value.add_argument("--asset", dest="asset_ref", help="reference to an asset's main object (.asset, .uxml, .uss, .tss)")
    value.add_argument("--remove", action="store_true", help="remove the field (and its list items)")
    p.set_defaults(func=cmd_set_field)

    p = sub.add_parser("add-component", help="add a MonoBehaviour to a GameObject of a prefab or scene")
    p.add_argument("asset")
    p.add_argument("object", help="GameObject name")
    p.add_argument("script", help="the script's .cs path")
    p.set_defaults(func=cmd_add_component)

    args = parser.parse_args()
    sys.exit(args.func(args))


if __name__ == "__main__":
    main()
