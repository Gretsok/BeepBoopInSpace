#!/usr/bin/env python3
"""Photoshop mockups (.psd) of Beep Boop dans l'espace, and captures of the game compared with them.

  setup                                   create the Python environment of the other commands (once)
  tree <psd> [--path GLOB] [--depth N]    layer tree: kind, visibility, box, opacity, blend, text, fill
  info <psd> GLOB...                      details of matching layers: fill, stroke, effects, text style in px
  render <psd> [--exclusive GLOB]         Photoshop's composite, then one render per layer matching GLOB
         [--show GLOB] [--hide GLOB]      shown alone among the matches (the screen's configurations)
  layer <psd> GLOB [--white-mask]         export a layer's pixels (e.g. a pattern to rebuild in the game)
  sheet <out.png> <png>...                contact sheet of sprites, with size and opaque box of each
  crop <img> <out> X0 Y0 X1 Y1 [--scale]  crop (and scale) an image
  compare <mock> <capture> <out>          mockup, capture and their 50% blend stacked, mean difference
          [--box X0 Y0 X1 Y1] [--scale S]
  inkbox <img> X0 Y0 X1 Y1 [--dark]       bounding box of light (or dark) pixels: where a text really is
  pixel <img> X,Y...                      colors of pixels

Run from BeepBoopInSpaceUnityProject/. Paths of layers are their names from the top of the file joined
by "/" ("CHARACTER SELECTION/3 PLAYERS/PLAYERS SETUP/J1"); in a GLOB, * and ? stay within a name, ** crosses
groups ("**/J1/*" = the children of every J1).
Images go to Logs/mockups/<psd name>/ unless --out says otherwise (Logs/ is ignored by git).
"""
import argparse
import re
import os
import subprocess
import sys
from pathlib import Path

PROJECT = Path(__file__).resolve().parents[4]
VENV = PROJECT / "Library" / "claude-psd-venv"
VENV_PYTHON = VENV / ("Scripts/python.exe" if os.name == "nt" else "bin/python")
PACKAGES = ["psd-tools[composite]", "numpy", "scipy", "pillow", "fonttools"]


def run_in_venv():
    """Every command but setup needs psd-tools and Pillow: run again with the environment's Python."""
    if Path(sys.prefix).resolve() == VENV.resolve():
        return
    if not VENV_PYTHON.exists():
        sys.exit(f"No environment yet: run `python {Path(__file__).as_posix()} setup` once (needs internet, ~1 min).")
    sys.exit(subprocess.run([str(VENV_PYTHON), __file__, *sys.argv[1:]]).returncode)


def setup(_):
    if not VENV_PYTHON.exists():
        subprocess.run([sys.executable, "-m", "venv", str(VENV)], check=True)
    subprocess.run([str(VENV_PYTHON), "-m", "pip", "install", "--quiet", "--disable-pip-version-check", *PACKAGES], check=True)
    print(f"Environment ready: {VENV}")


# --- PSD -------------------------------------------------------------------------------------------------------

def open_psd(path):
    from psd_tools import PSDImage
    return PSDImage.open(path)


def walk(group, prefix=""):
    """(path, layer, depth) of every layer, depth first, in Photoshop's order from the bottom."""
    for layer in group:
        path = f"{prefix}/{layer.name}" if prefix else layer.name
        yield path, layer, path.count("/")
        if layer.is_group():
            yield from walk(layer, path)


def glob_match(path, glob):
    """Like file names: * and ? stay within a layer name, ** crosses groups."""
    pattern = ""
    i = 0
    while i < len(glob):
        if glob.startswith("**", i):
            pattern += ".*"
            i += 2
        elif glob[i] == "*":
            pattern += "[^/]*"
            i += 1
        elif glob[i] == "?":
            pattern += "[^/]"
            i += 1
        else:
            pattern += re.escape(glob[i])
            i += 1
    return re.fullmatch(pattern, path) is not None


def matches(path, globs):
    return any(glob_match(path, glob) for glob in globs)


def hex_rgb(r, g, b):
    r, g, b = (int(round(v)) for v in (r, g, b))
    return f"#{r:02X}{g:02X}{b:02X} ({r},{g},{b})"


def descriptor_color(color):
    if color is None:
        return "?"
    values = {(k.decode() if isinstance(k, bytes) else str(k)).strip(): v for k, v in color.items()}
    if "Rd" in values:
        return hex_rgb(float(values["Rd"]), float(values["Grn"]), float(values["Bl"]))
    if "redFloat" in values:
        return hex_rgb(float(values["redFloat"]) * 255, float(values["greenFloat"]) * 255, float(values["blueFloat"]) * 255)
    return str(values)


def fill_opacity(layer):
    from psd_tools.constants import Tag
    block = layer.tagged_blocks.get(Tag.BLEND_FILL_OPACITY) if layer.tagged_blocks else None
    return None if block is None else block.data


def text_style(layer):
    """Font, size in px of the canvas (Photoshop's size times the layer's scale), color, text."""
    try:
        style = layer.engine_dict["StyleRun"]["RunArray"][0]["StyleSheet"]["StyleSheetData"]
        fonts = layer.resource_dict["FontSet"]
        font = str(fonts[style.get("Font", 0)]["Name"]).strip("'")
        scale = float(layer.transform[3])
        size = float(style.get("FontSize", 0)) * scale
        fill = style.get("FillColor", {}).get("Values")
        color = hex_rgb(*(float(v) * 255 for v in list(fill)[1:])) if fill is not None else "?"
        extras = [f"{key}={style[key]}" for key in ("FauxBold", "FauxItalic", "Tracking") if style.get(key)]
        text = str(layer.text).replace("\r", " / ")
        return f'"{text}" {font} {size:.1f}px {color}' + (" " + " ".join(extras) if extras else "")
    except Exception as error:  # an unusual text layer: still list it
        return f'"{getattr(layer, "text", "?")}" (style unreadable: {error})'


def shape_style(layer):
    from psd_tools.constants import Tag
    parts = []
    try:
        # A shape's fill is in its 'vscg' block (psd-tools names it VECTOR_STROKE_CONTENT_DATA).
        fill = layer.tagged_blocks.get_data(Tag.SOLID_COLOR_SHEET_SETTING) or layer.tagged_blocks.get_data(Tag.VECTOR_STROKE_CONTENT_DATA)
        if fill is not None and b"Clr " in fill:
            parts.append(f"fill {descriptor_color(fill[b'Clr '])}")
    except Exception:
        pass
    try:
        stroke = layer.stroke
        if stroke is not None and stroke.enabled:
            content = stroke.content
            color = descriptor_color(content[b"Clr "]) if b"Clr " in content else "?"
            parts.append(f"stroke {float(stroke.line_width):.1f}px {stroke.line_alignment.name.lower()} {color}")
    except Exception:
        pass
    try:
        for origination in layer.origination:
            kind = type(origination).__name__
            if kind != "Invalidated":
                box = getattr(origination, "bbox", None)
                if box:
                    parts.append(f"{kind.lower()} {box[2] - box[0]:.1f}x{box[3] - box[1]:.1f}")
    except Exception:
        pass
    return " ".join(parts)


# Blend modes of layer effects, as Photoshop's descriptors spell them.
BLEND_CODES = {b"norm": "normal", b"mul ": "multiply", b"scrn": "screen", b"over": "overlay", b"lite": "lighten",
               b"dark": "darken", b"vLit": "vivid light", b"lLit": "linear light", b"pLit": "pin light",
               b"colorDodge": "color dodge", b"colorBurn": "color burn", b"linearDodge": "linear dodge (add)"}


def effects(layer, detailed):
    names = []
    try:
        for effect in layer.effects:
            if not effect.enabled:
                continue
            name = type(effect).__name__
            if detailed:
                values = []
                for attribute in ("color", "opacity", "size", "choke", "distance", "angle", "depth", "blend_mode",
                                  "highlight_opacity", "shadow_opacity", "altitude"):
                    try:
                        value = getattr(effect, attribute)
                    except Exception:
                        continue
                    if value is None:
                        continue
                    if attribute == "color" and hasattr(value, "items"):
                        value = descriptor_color(value)
                    elif hasattr(value, "value"):
                        value = value.value
                    if isinstance(value, bytes):
                        value = BLEND_CODES.get(value, value.decode(errors="replace"))
                    elif isinstance(value, float):
                        value = round(value, 1)
                    values.append(f"{attribute}={value}")
                name += "(" + ", ".join(values) + ")"
            names.append(name)
    except Exception:
        pass
    return names


def describe(layer, detailed=False):
    left, top, right, bottom = layer.bbox
    parts = [f"[{layer.kind}]", repr(layer.name)]
    if not layer.visible:
        parts.append("HIDDEN")
    if right > left:
        parts.append(f"{right - left}x{bottom - top}@{left},{top}")
    if layer.opacity != 255:
        parts.append(f"opacity {layer.opacity * 100 // 255}%")
    fill = fill_opacity(layer)
    if fill is not None and fill != 255:
        parts.append(f"fill-opacity {fill * 100 // 255}%")
    blend = getattr(layer.blend_mode, "name", str(layer.blend_mode))
    if blend not in ("NORMAL", "PASS_THROUGH"):
        parts.append(blend.lower())
    if getattr(layer, "clipping", False):
        parts.append("clipped")
    if layer.kind == "type":
        parts.append(text_style(layer))
    elif layer.kind == "shape":
        parts.append(shape_style(layer))
    found = effects(layer, detailed)
    if found:
        parts.append("fx " + ("; ".join(found) if detailed else ",".join(found)))
    return " ".join(part for part in parts if part)


def tree(args):
    psd = open_psd(args.psd)
    print(f"{args.psd}: {psd.width}x{psd.height}")
    for path, layer, depth in walk(psd):
        if args.path and not (glob_match(path, args.path) or any(
                glob_match(path[:i], args.path) for i in range(len(path)) if path[i] == "/")):
            continue
        if args.depth is not None and depth > args.depth:
            continue
        print("  " * depth + describe(layer))


def info(args):
    psd = open_psd(args.psd)
    found = False
    for path, layer, _ in walk(psd):
        if matches(path, args.globs) and not layer.is_group():
            found = True
            print(f"{path}\n    {describe(layer, detailed=True)}")
    if not found:
        sys.exit("No layer matches. `tree` lists the paths.")


def out_folder(args):
    folder = Path(args.out) if args.out else PROJECT / "Logs" / "mockups" / Path(args.psd).stem
    folder.mkdir(parents=True, exist_ok=True)
    return folder


def file_name(path):
    return "".join(c if c.isalnum() or c in "-_" else "_" for c in path.replace("/", "__")).strip("_") or "layer"


def set_visible(layer, visible):
    """A layer shows only if its groups do: showing one shows its parents too."""
    layer.visible = visible
    parent = layer.parent
    while visible and parent is not None and parent.kind != "psdimage":
        parent.visible = True
        parent = parent.parent


def render(args):
    psd = open_psd(args.psd)
    folder = out_folder(args)
    psd.topil().save(folder / "merged.png")
    print(f"{folder / 'merged.png'}  (Photoshop's own composite: the layers visible when the file was saved, effects included)")
    layers = list(walk(psd))
    for glob in args.show or []:
        for path, layer, _ in layers:
            if glob_match(path, glob):
                set_visible(layer, True)
    for glob in args.hide or []:
        for path, layer, _ in layers:
            if glob_match(path, glob):
                layer.visible = False
    variants = [(path, layer) for path, layer, _ in layers if args.exclusive and glob_match(path, args.exclusive)]
    if args.exclusive and not variants:
        sys.exit("No layer matches --exclusive. `tree` lists the paths.")
    for path, layer in variants or [("render", None)]:
        for _, other in variants:
            other.visible = False
        if layer is not None:
            set_visible(layer, True)
        target = folder / f"{file_name(path)}.png"
        psd.composite(force=True).save(target)
        print(f"{target}  ({path})")
    print("psd-tools renders without the bevels and glows, and its gradients can differ: merged.png is the reference.")


def layer_export(args):
    import numpy as np
    from PIL import Image
    psd = open_psd(args.psd)
    folder = out_folder(args)
    exported = 0
    for path, layer, _ in walk(psd):
        if not glob_match(path, args.glob) or layer.is_group():
            continue
        image = layer.topil()
        if image is None:
            continue
        image = image.convert("RGBA")
        canvas = Image.new("RGBA", (psd.width, psd.height))
        canvas.paste(image, (layer.left, layer.top))
        pixels = np.asarray(canvas).astype(float)
        if args.white_mask:
            # Coverage = luminance x alpha, as white: what a "color dodge" or "screen" layer adds.
            coverage = pixels[..., :3].mean(axis=2) / 255 * pixels[..., 3] / 255
            mask = np.zeros(pixels.shape, dtype=np.uint8)
            mask[..., :3] = 255
            mask[..., 3] = np.round(coverage * 255).astype(np.uint8)
            canvas = Image.fromarray(mask)
        target = folder / f"{file_name(path)}{'-mask' if args.white_mask else ''}.png"
        canvas.save(target, optimize=True)
        alpha = np.asarray(canvas)[..., 3]
        print(f"{target}  {describe(layer)}  alpha mean {alpha.mean() / 255:.2f}, {target.stat().st_size // 1024} KB")
        exported += 1
    if not exported:
        sys.exit("No layer with pixels matches. `tree` lists the paths.")


# --- Images ----------------------------------------------------------------------------------------------------

def sheet(args):
    from PIL import Image, ImageDraw
    cell, columns = 300, 5
    rows = (len(args.images) + columns - 1) // columns
    board = Image.new("RGBA", (columns * cell, rows * (cell + 40)), (60, 60, 70, 255))
    draw = ImageDraw.Draw(board)
    for i, path in enumerate(args.images):
        image = Image.open(path).convert("RGBA")
        box = image.getbbox()
        print(f"{Path(path).name}: {image.width}x{image.height}, opaque box {box}"
              f" -> drawn at half size: {image.width / 2:g}x{image.height / 2:g}")
        x, y = (i % columns) * cell, (i // columns) * (cell + 40)
        for cy in range(0, cell, 20):  # checkerboard, so that white sprites show
            for cx in range(0, cell, 20):
                if (cx + cy) // 20 % 2:
                    draw.rectangle([x + cx, y + cy, x + cx + 19, y + cy + 19], fill=(80, 80, 90, 255))
        thumb = image.copy()
        thumb.thumbnail((cell - 10, cell - 10))
        board.alpha_composite(thumb, (x + 5, y + 5))
        draw.text((x + 5, y + cell + 2), Path(path).name[:42], fill=(255, 255, 255, 255))
        draw.text((x + 5, y + cell + 18), f"{image.width}x{image.height}", fill=(255, 255, 0, 255))
    board.save(args.out)
    print(f"Sheet: {args.out}")


def crop(args):
    from PIL import Image
    image = Image.open(args.image).convert("RGBA").crop(tuple(args.box))
    if args.scale != 1:
        image = image.resize((int(image.width * args.scale), int(image.height * args.scale)), Image.LANCZOS)
    image.save(args.out)
    print(args.out)


def compare(args):
    import numpy as np
    from PIL import Image
    mock = Image.open(args.mock).convert("RGB")
    capture = Image.open(args.capture).convert("RGB").resize(mock.size, Image.LANCZOS)
    box = tuple(args.box) if args.box else (0, 0, mock.width, mock.height)
    a, b = mock.crop(box), capture.crop(box)
    width, height = a.size
    board = Image.new("RGB", (width, height * 3 + 8), (255, 0, 255))
    board.paste(a, (0, 0))
    board.paste(b, (0, height + 4))
    board.paste(Image.blend(a, b, 0.5), (0, 2 * height + 8))
    if args.scale != 1:
        board = board.resize((int(board.width * args.scale), int(board.height * args.scale)), Image.LANCZOS)
    board.save(args.out)
    difference = np.abs(np.asarray(a).astype(int) - np.asarray(b).astype(int)).mean()
    print(f"{args.out}: mockup, capture, 50% blend. Mean difference {difference:.1f} / 255")


def inkbox(args):
    import numpy as np
    from PIL import Image
    pixels = np.asarray(Image.open(args.image).convert("RGB")).astype(int)
    x0, y0, x1, y1 = args.box
    region = pixels[y0:y1, x0:x1]
    mask = region.max(axis=2) < args.threshold if args.dark else region.min(axis=2) > args.threshold
    ys, xs = np.nonzero(mask)
    if len(xs) == 0:
        sys.exit("No pixel past the threshold in that box.")
    print(f"box {x0 + xs.min()},{y0 + ys.min()} -> {x0 + xs.max() + 1},{y0 + ys.max() + 1}"
          f"  size {xs.max() - xs.min() + 1}x{ys.max() - ys.min() + 1}")


def pixel(args):
    from PIL import Image
    image = Image.open(args.image).convert("RGBA")
    for point in args.points:
        x, y = (int(v) for v in point.split(","))
        r, g, b, a = image.getpixel((x, y))
        print(f"{x},{y}: {hex_rgb(r, g, b)} alpha {a}")


def main():
    sys.stdout.reconfigure(encoding="utf-8")  # layer names hold é, ♦...
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    commands = parser.add_subparsers(dest="command", required=True)

    commands.add_parser("setup", help="create the Python environment (psd-tools, Pillow, numpy)").set_defaults(func=setup)

    p = commands.add_parser("tree", help="layer tree")
    p.add_argument("psd")
    p.add_argument("--path", help="only this subtree (glob on the layer path)")
    p.add_argument("--depth", type=int)
    p.set_defaults(func=tree)

    p = commands.add_parser("info", help="details of matching layers")
    p.add_argument("psd")
    p.add_argument("globs", nargs="+")
    p.set_defaults(func=info)

    p = commands.add_parser("render", help="composite and configurations")
    p.add_argument("psd")
    p.add_argument("--exclusive", help="layers shown one at a time, the others hidden (e.g. 'SCREEN/* PLAYERS')")
    p.add_argument("--show", action="append", help="layers to show (with their parents)")
    p.add_argument("--hide", action="append", help="layers to hide")
    p.add_argument("--out")
    p.set_defaults(func=render)

    p = commands.add_parser("layer", help="export a layer's pixels")
    p.add_argument("psd")
    p.add_argument("glob")
    p.add_argument("--white-mask", action="store_true", help="white, with alpha = luminance x alpha")
    p.add_argument("--out")
    p.set_defaults(func=layer_export)

    p = commands.add_parser("sheet", help="contact sheet of sprites")
    p.add_argument("out")
    p.add_argument("images", nargs="+")
    p.set_defaults(func=sheet)

    p = commands.add_parser("crop", help="crop an image")
    p.add_argument("image")
    p.add_argument("out")
    p.add_argument("box", nargs=4, type=int, metavar=("X0", "Y0", "X1", "Y1"))
    p.add_argument("--scale", type=float, default=1)
    p.set_defaults(func=crop)

    p = commands.add_parser("compare", help="mockup vs capture")
    p.add_argument("mock")
    p.add_argument("capture")
    p.add_argument("out")
    p.add_argument("--box", nargs=4, type=int, metavar=("X0", "Y0", "X1", "Y1"))
    p.add_argument("--scale", type=float, default=1)
    p.set_defaults(func=compare)

    p = commands.add_parser("inkbox", help="bounding box of light or dark pixels")
    p.add_argument("image")
    p.add_argument("box", nargs=4, type=int, metavar=("X0", "Y0", "X1", "Y1"))
    p.add_argument("--threshold", type=int, default=150, help="min channel above it (light), or max below it with --dark")
    p.add_argument("--dark", action="store_true")
    p.set_defaults(func=inkbox)

    p = commands.add_parser("pixel", help="colors of pixels")
    p.add_argument("image")
    p.add_argument("points", nargs="+", metavar="X,Y")
    p.set_defaults(func=pixel)

    args = parser.parse_args()
    if args.command != "setup":
        run_in_venv()
    args.func(args)


if __name__ == "__main__":
    main()
