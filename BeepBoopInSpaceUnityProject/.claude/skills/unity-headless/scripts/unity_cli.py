#!/usr/bin/env python3
"""Headless Unity toolbox for this project (Windows, Python 3 standard library only).

Run `python unity_cli.py <command> --help` for the options of each command:
  status          project, Unity version and executable, editor open or not
  quick-compile   compile the project's C# in seconds with Unity's compiler (no Unity launch)
  compile         full headless import + compile (1-3 min), the authoritative check
  test            run Unity Test Framework tests headless, then summarize the results
  wait            block until a process exits (for detached runs)
  results         summarize a test results XML
  log-errors      distinct errors/exceptions (and optionally warnings) of a Unity log, with counts
  log-flow        FlowMachine transitions and [Scenario] steps of a log, in order
  build           Windows player build via Game.Tools.Build.Editor.CommandLineBuild, optional zip
  run-build       launch a built player for N seconds, then triage its log
  relay-test      two copies of a build play a real room (Unity services, Relay) by themselves, and quit
  screenshots     run the main menu scenario, which captures every menu screen, and list the PNGs

Runs go to <project>/Logs/claude-runs/<timestamp>-<name>/ (Logs/ is ignored by git).
Exit codes: 0 success, 1 failure (errors, failed tests, build failed), 3 the editor has the project open.
"""
import argparse
import collections
import ctypes
import datetime
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
import time
import xml.etree.ElementTree as ET
from pathlib import Path

PROJECT = Path(os.environ.get("BEEPBOOP_PROJECT", Path(__file__).resolve().parents[4]))
RUNS_DIR = PROJECT / "Logs" / "claude-runs"
BUILD_METHOD = "Game.Tools.Build.Editor.CommandLineBuild.BuildWindows"

if sys.stdout.encoding and sys.stdout.encoding.lower() != "utf-8":
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")


# ----------------------------------------------------------------------------- project facts

def editor_version():
    text = (PROJECT / "ProjectSettings" / "ProjectVersion.txt").read_text(encoding="utf-8")
    return re.search(r"m_EditorVersion:\s*(\S+)", text).group(1)


def hub_editor_roots():
    roots = [Path(r"C:\Program Files\Unity\Hub\Editor")]
    secondary = Path(os.environ.get("APPDATA", "")) / "UnityHub" / "secondaryInstallPath.json"
    if secondary.is_file():
        try:
            path = json.loads(secondary.read_text(encoding="utf-8"))
            if path:
                roots.append(Path(path))
        except (ValueError, OSError):
            pass
    return roots


def unity_exe(version=None):
    if os.environ.get("UNITY_EXE"):
        return Path(os.environ["UNITY_EXE"])
    version = version or editor_version()
    for root in hub_editor_roots():
        exe = root / version / "Editor" / "Unity.exe"
        if exe.is_file():
            return exe
    installed = sorted({p.name for root in hub_editor_roots() if root.is_dir() for p in root.iterdir()})
    sys.exit(f"Unity {version} not found (installed: {', '.join(installed) or 'none'}). "
             f"Install it with Unity Hub, or set UNITY_EXE.")


def player_settings():
    text = (PROJECT / "ProjectSettings" / "ProjectSettings.asset").read_text(encoding="utf-8")
    get = lambda key: re.search(rf"^\s*{key}:\s*(.*)$", text, re.M).group(1).strip()
    return get("companyName"), get("productName")


def persistent_data_dir():
    company, product = player_settings()
    return Path.home() / "AppData" / "LocalLow" / company / product


def player_prefs_key():
    company, product = player_settings()
    return rf"Software\{company}\{product}"


def snapshot_player_prefs():
    """The built game's PlayerPrefs (HKCU registry values: screen mode, resolution, sessions...), or None."""
    import winreg
    values = {}
    try:
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER, player_prefs_key()) as key:
            index = 0
            while True:
                try:
                    name, data, kind = winreg.EnumValue(key, index)
                except OSError:
                    break
                values[name] = (data, kind)
                index += 1
    except FileNotFoundError:
        return None
    return values


def restore_player_prefs(snapshot):
    """A check run must not change the user's settings: Unity saves the screen options it was launched with."""
    import winreg
    current = snapshot_player_prefs()
    if current == snapshot:
        return False
    if snapshot is None:
        winreg.DeleteKey(winreg.HKEY_CURRENT_USER, player_prefs_key())
        return True
    with winreg.CreateKey(winreg.HKEY_CURRENT_USER, player_prefs_key()) as key:
        for name in (current or {}):
            if name not in snapshot:
                winreg.DeleteValue(key, name)
        for name, (data, kind) in snapshot.items():
            if (current or {}).get(name) != (data, kind):
                winreg.SetValueEx(key, name, 0, kind, data)
    return True


def editor_open():
    """True when an editor holds the project lock (a stale lockfile can be opened, a held one cannot)."""
    lock = PROJECT / "Temp" / "UnityLockfile"
    if not lock.exists():
        return False
    try:
        with open(lock, "a"):
            return False
    except PermissionError:
        return True


def require_editor_closed():
    if editor_open():
        print("Unity already has this project open (the editor, or another headless run): a second instance can't run on it.\n"
              "Ask the user to close the editor, or wait for the other run (quick-compile works meanwhile).")
        sys.exit(3)


def new_run_dir(name):
    # A test filter is a regex: its * | ? and the like aren't allowed in a Windows folder name.
    name = re.sub(r'[<>:"/\\|?*]+', "_", name).strip(" ._") or "run"
    path = RUNS_DIR / f"{datetime.datetime.now():%Y%m%d-%H%M%S}-{name}"
    path.mkdir(parents=True, exist_ok=True)
    return path


# ----------------------------------------------------------------------------- processes

DETACHED_PROCESS, CREATE_NEW_PROCESS_GROUP = 0x00000008, 0x00000200


def launch_unity(args, log, detach=False):
    cmd = [str(unity_exe()), "-batchmode", "-projectPath", str(PROJECT), *args, "-logFile", str(log)]
    flags = DETACHED_PROCESS | CREATE_NEW_PROCESS_GROUP if detach else 0
    return subprocess.Popen(cmd, creationflags=flags, close_fds=True)


def wait_for_pid(pid):
    """Waits for any process (not only our children) and returns its exit code, or None if unknown."""
    kernel32 = ctypes.windll.kernel32
    handle = kernel32.OpenProcess(0x00100000 | 0x1000, False, pid)  # SYNCHRONIZE | QUERY_LIMITED_INFORMATION
    if not handle:
        return None
    try:
        kernel32.WaitForSingleObject(handle, 0xFFFFFFFF)
        code = ctypes.c_ulong()
        return code.value if kernel32.GetExitCodeProcess(handle, ctypes.byref(code)) else None
    finally:
        kernel32.CloseHandle(handle)


# ----------------------------------------------------------------------------- log analysis

ERROR_MARKERS = ("Debug:LogError", "Debug:LogException", "Debug:LogAssertion", "Logger:LogException")
EXCEPTION_LINE = re.compile(r"^(?:[\w.`]*Exception|[\w.`]+Error): ")
COMPILE_ERROR = re.compile(r"^(?P<file>[^(]+)\((?P<line>\d+),(?P<col>\d+)\): error (?P<code>CS\d+): (?P<msg>.*)$")
NOISE = ("UnityEngine.Debug:", "UnityEngine.StackTraceUtility", "UnityEngine.DebugLogHandler", "UnityEngine.Logger:",
         "(Filename:", "UnityEngine.SetupCoroutine", "UnityEngineInternal.", "System.")


def as_path(value):
    """Accepts Git Bash paths (/c/Users/...) as well as Windows ones."""
    match = re.match(r"^/([a-zA-Z])/(.*)$", str(value))
    return Path(f"{match.group(1)}:/{match.group(2)}") if match else Path(value)


def read_log(path):
    return as_path(path).read_text(encoding="utf-8", errors="replace").splitlines()


def log_blocks(lines):
    block = []
    for line in lines:
        if line.strip():
            block.append(line)
        elif block:
            yield block
            block = []
    if block:
        yield block


def first_project_frame(block):
    for line in block:
        match = re.search(r"\(at (Assets/[^)]+)\)", line) or re.search(r" in .*?(Assets[\\/][^:]+:\d+)", line)
        if match:
            return match.group(1).replace("\\", "/")
    return None


STACK_START = ("UnityEngine.Debug", "UnityEngine.StackTraceUtility", "UnityEngine.DebugLogHandler", "UnityEngine.Logger")


def classify(block):
    """Returns (kind, message) for an error or warning block, else (None, None)."""
    for line in block:
        if EXCEPTION_LINE.match(line):
            return "error", line
    text = "\n".join(block)
    if any(marker in text for marker in ERROR_MARKERS):
        kind = "error"
    elif "Debug:LogWarning" in text:
        kind = "warning"
    else:
        return None, None
    # Other messages can precede this one without a blank line: the message is the line before its stack trace.
    start = next((i for i, line in enumerate(block) if i > 0 and line.startswith(STACK_START)), None)
    return kind, block[start - 1] if start else block[0]


def analyze_log(path, include_warnings=False):
    lines = read_log(path)
    compile_errors = collections.OrderedDict()
    for line in lines:
        match = COMPILE_ERROR.match(line.strip())
        if match:
            compile_errors.setdefault(line.strip(), 0)
            compile_errors[line.strip()] += 1
    counts = collections.Counter()
    where = {}
    first_error = None
    for block in log_blocks(lines):
        kind, message = classify(block)
        if kind is None or (kind == "warning" and not include_warnings):
            continue
        key = (kind, message.strip()[:220])
        counts[key] += 1
        where.setdefault(key, first_project_frame(block))
        if kind == "error" and first_error is None:
            # Outside the project too (e.g. a URP render feature): the first frame says which system failed first.
            frame = next((l.strip() for l in block if l.strip().startswith("at ") or "(at " in l), None)
            first_error = (key[1], frame)
    return list(compile_errors), counts, where, first_error


def print_log_analysis(path, include_warnings=False, top=25):
    compile_errors, counts, where, first_error = analyze_log(path, include_warnings)
    if compile_errors:
        print(f"{len(compile_errors)} distinct compile error(s):")
        for line in compile_errors[:top]:
            print(f"  {line}")
    if first_error and len([k for k in counts if k[0] == "error"]) > 1:
        print(f"First error logged (often the cause of the others): {first_error[0]}"
              + (f"\n         {first_error[1][:200]}" if first_error[1] else ""))
    if counts:
        total = sum(n for (kind, _), n in counts.items() if kind == "error")
        print(f"{total} runtime error(s)/exception(s) logged ({len([k for k in counts if k[0] == 'error'])} distinct)"
              + (f", {sum(n for (k, _), n in counts.items() if k == 'warning')} warning(s)" if include_warnings else "") + ":")
        for (kind, message), n in counts.most_common(top):
            frame = where[(kind, message)]
            print(f"  {n:>4}x {kind:7} {message}" + (f"\n         at {frame}" if frame else ""))
    if not compile_errors and not counts:
        print("No errors found in the log.")
    return bool(compile_errors) or any(kind == "error" for kind, _ in counts)


# ----------------------------------------------------------------------------- test results

def as_float(value):
    return float((value or "0").replace(",", "."))  # the Test Runner writes durations with the OS locale


def summarize_results(xml_path, log_path=None):
    root = ET.parse(as_path(xml_path)).getroot()
    print(f"Result: {root.get('result')} - {root.get('passed')}/{root.get('total')} passed, "
          f"{root.get('failed')} failed, {as_float(root.get('duration')):.0f}s")
    for case in root.iter("test-case"):
        print(f"  {case.get('result'):7} {as_float(case.get('duration')):6.1f}s  {case.get('fullname')}")
        failure = case.find("failure/message")
        if failure is not None and failure.text:
            for block in failure.text.split("\n----\n")[:8]:
                lines = [l.strip() for l in block.splitlines() if l.strip()]
                frame = next((l for l in lines[1:] if "(at Assets/" in l), None)
                print("           " + "\n           ".join(l[:230] for l in lines[:2 if "error(s) logged" in lines[0] else 1]))
                if frame:
                    print(f"             {frame[:230]}")
        output = case.find("output")
        if output is not None and output.text:
            for line in sorted({l.strip() for l in output.text.splitlines() if "Known issue" in l and l.startswith("[")}):
                print(f"           {line}")
    if log_path and as_path(log_path).is_file():
        scores = [l for l in read_log(log_path) if l.startswith("[Scenario]") and ("Scores:" in l or "Tournament over" in l)]
        if scores:
            print("Games played:")
            for line in scores:
                print("  " + line.replace("SwampArena - ", "").replace(" - LevelDataAsset", ""))
    return root.get("failed") == "0" and root.get("result", "").startswith("Passed")


def clean_test_results_copy(since):
    """The Test Runner also writes TestResults.xml into the game's persistent data folder; remove ours."""
    stray = persistent_data_dir() / "TestResults.xml"
    if stray.is_file() and stray.stat().st_mtime >= since:
        stray.unlink()


# ----------------------------------------------------------------------------- quick compile

def bee_dag_dir():
    """The editor's build graph: a player build adds its own graph (no editor assemblies, no tests, no UNITY_EDITOR)."""
    dags = [p for p in (PROJECT / "Library" / "Bee" / "artifacts").glob("*.dag") if (p / "Assembly-CSharp-Editor.rsp").is_file()]
    return max(dags, key=lambda p: (p / "Assembly-CSharp-Editor.rsp").stat().st_mtime) if dags else None


def assembly_of(path):
    """Unity's rule: the nearest .asmdef up the folders, else Assembly-CSharp(-Editor/-firstpass)."""
    folder = (PROJECT / path).parent
    while folder != PROJECT and folder.name != "":
        asmdef = next(folder.glob("*.asmdef"), None)
        if asmdef:
            return json.loads(asmdef.read_text(encoding="utf-8-sig"))["name"]
        folder = folder.parent
    parts = Path(path).parts
    firstpass = len(parts) > 1 and parts[1] in ("Plugins", "Standard Assets")
    return f"Assembly-CSharp{'-firstpass' if firstpass else ''}{'-Editor' if 'Editor' in parts else ''}"


def rsp_sources(rsp):
    return [l.strip().strip('"') for l in rsp.read_text(encoding="utf-8").splitlines() if l.strip().startswith('"Assets/')]


def cmd_quick_compile(args):
    dag = bee_dag_dir()
    if not dag:
        sys.exit("No compiled state in Library/Bee yet: run `compile` once (it needs the editor closed).")
    editor = unity_exe().parent
    dotnet = editor / "Data" / "DotNetSdk" / "dotnet.exe"
    csc = next((editor / "Data" / "DotNetSdk" / "sdk").glob("*/Roslyn/bincore/csc.dll"), None)
    if not dotnet.is_file() or not csc:
        sys.exit(f"Unity's Roslyn compiler not found under {editor / 'Data' / 'DotNetSdk'}.")

    rsps = {p.stem: p for p in dag.glob("*.rsp") if rsp_sources(p)}
    if args.assemblies:
        wanted = set(args.assemblies.split(","))
        rsps = {name: p for name, p in rsps.items() if name in wanted}

    listed = {s for p in dag.glob("*.rsp") for s in rsp_sources(p)}
    on_disk = {p.relative_to(PROJECT).as_posix() for p in (PROJECT / "Assets").rglob("*.cs")}
    added, removed = sorted(on_disk - listed), sorted(listed - on_disk)
    extra_sources = collections.defaultdict(list)
    for path in added:
        extra_sources[assembly_of(path)].append(path)
    removed_set = set(removed)
    if added or removed:
        print(f"Since the last Unity compile: {len(added)} new and {len(removed)} deleted .cs file(s).")
        for path in added:
            print(f"  + {path} -> {assembly_of(path)}")
        for path in removed:
            print(f"  - {path}")

    unsafe_assemblies = set()
    for asmdef in (PROJECT / "Assets").rglob("*.asmdef"):
        definition = json.loads(asmdef.read_text(encoding="utf-8-sig"))
        if definition.get("allowUnsafeCode"):
            unsafe_assemblies.add(definition["name"])

    out_dir = Path(tempfile.mkdtemp(prefix="quick-compile-"))
    compiled, failed, pending = {}, [], dict(rsps)
    # Compile dependencies first and point dependents at the fresh reference assemblies.
    while pending:
        ready = [n for n, p in pending.items()
                 if not any(f"/{dep}.ref.dll" in p.read_text(encoding="utf-8") for dep in pending if dep != n)]
        name = (ready or list(pending))[0]
        rsp = pending.pop(name)
        text = rsp.read_text(encoding="utf-8")
        # Lambdas: Windows paths in a replacement string would be read as regex escapes.
        text = re.sub(r'^-out:.*$', lambda _: f'-out:"{out_dir / (name + ".dll")}"', text, flags=re.M)
        text = re.sub(r'^-refout:.*$', lambda _: f'-refout:"{out_dir / (name + ".ref.dll")}"', text, flags=re.M)
        for dep, ref in compiled.items():
            text = re.sub(rf'^-r:".*?/{re.escape(dep)}\.ref\.dll"$', lambda _, ref=ref: f'-r:"{ref}"', text, flags=re.M)
        text = "\n".join(l for l in text.splitlines() if l.strip().strip('"') not in removed_set)
        text += "".join(f'\n"{path}"' for path in extra_sources.get(name, []))
        # An asmdef that has just allowed unsafe code: the editor's arguments don't say so yet.
        if name in unsafe_assemblies and not re.search(r'^[-/]unsafe\b', text, flags=re.M):
            text += "\n-unsafe"
        local_rsp = out_dir / f"{name}.rsp"
        local_rsp.write_text(text, encoding="utf-8")
        result = subprocess.run([str(dotnet), "exec", str(csc), "-nostdlib", "-noconfig", f"@{local_rsp}"],
                                cwd=PROJECT, capture_output=True, text=True, encoding="utf-8", errors="replace")
        output = result.stdout + result.stderr
        errors = sorted({l.strip() for l in output.splitlines() if "error CS" in l})
        warnings = sorted({l.strip() for l in output.splitlines()
                           if "warning CS" in l and (args.all_warnings or l.strip().startswith("Assets"))})
        status = "ok" if result.returncode == 0 else "FAILED"
        print(f"{name}: {status}, {len(errors)} error(s), {len(warnings)} warning(s)")
        for line in errors + warnings:
            print(f"  {line}")
        if result.returncode == 0:
            compiled[name] = out_dir / f"{name}.ref.dll"
        else:
            failed.append(name)
    shutil.rmtree(out_dir, ignore_errors=True)
    return 1 if failed else 0


# ----------------------------------------------------------------------------- commands

def cmd_status(args):
    version = editor_version()
    company, product = player_settings()
    print(f"Project:         {PROJECT}")
    print(f"Unity version:   {version}")
    try:
        print(f"Unity exe:       {unity_exe(version)}")
    except SystemExit as missing:
        print(f"Unity exe:       MISSING - {missing}")
    print(f"Editor open:     {'yes - an editor or a headless run holds the project' if editor_open() else 'no'}")
    dag = bee_dag_dir()
    print(f"Quick compile:   {'available (' + dag.name + ')' if dag else 'not yet (run compile once)'}")
    print(f"Product:         {product} by {company}")
    print(f"Persistent data: {persistent_data_dir()}")
    print(f"Runs folder:     {RUNS_DIR}")
    return 0


def cmd_compile(args):
    require_editor_closed()
    run = new_run_dir("compile")
    log = run / "editor.log"
    start = time.time()
    code = launch_unity(["-quit"], log).wait()
    print(f"Unity exited with code {code} after {time.time() - start:.0f}s. Log: {log}")
    has_errors = print_log_analysis(log)
    if "Scripts have compiler errors" in log.read_text(encoding="utf-8", errors="replace"):
        has_errors = True
    return 1 if code != 0 or has_errors else 0


def cmd_test(args):
    require_editor_closed()
    run = new_run_dir(args.name or (args.filter or "all").split(".")[-1][:40].replace("(", "_").replace(")", "").replace('"', ""))
    log, results = run / "editor.log", run / "results.xml"
    unity_args = ["-runTests", "-testPlatform", args.platform, "-testResults", str(results)]
    if args.filter:
        unity_args += ["-testFilter", args.filter]
    start = time.time()
    process = launch_unity(unity_args, log, detach=args.detach)
    if args.detach:
        cli = Path(__file__).resolve()
        print(f"Started Unity (PID {process.pid}). Log: {log}\nResults: {results}\n"
              f"When it exits: python \"{cli}\" results \"{results}\" --log \"{log}\"")
        return 0
    code = process.wait()
    clean_test_results_copy(start)
    print(f"Unity exited with code {code} after {time.time() - start:.0f}s. Log: {log}")
    if not results.is_file():
        print("No results file: the run crashed or the code didn't compile.")
        print_log_analysis(log)
        return 1
    return 0 if summarize_results(results, log) else 1


def cmd_wait(args):
    code = wait_for_pid(args.pid)
    print(f"Process {args.pid} exited" + (f" with code {code}" if code is not None else " (or was not running)"))
    if args.cleanup_test_results:
        clean_test_results_copy(time.time() - 24 * 3600)
    return 0


def cmd_results(args):
    clean_test_results_copy(time.time() - 24 * 3600)
    return 0 if summarize_results(args.xml, args.log) else 1


def cmd_log_errors(args):
    return 1 if print_log_analysis(args.log, args.warnings, args.top) else 0


def cmd_log_flow(args):
    pattern = re.compile(r"^\[FM\] (?P<machine>\S+) - (?P<state>\S+) - ENTER|^\[Scenario\] (?P<step>.*)|^Loaded scene '(?P<scene>[^']+)'")
    for line in read_log(args.log):
        match = pattern.match(line)
        if not match:
            continue
        if match.group("machine") and (not args.machine or match.group("machine") == args.machine):
            print(f"  {match.group('machine')} -> {match.group('state')}")
        elif match.group("step"):
            print(f"[Scenario] {match.group('step')}")
        elif match.group("scene") and not match.group("scene").startswith("Temp/"):
            print(f"  scene {Path(match.group('scene')).stem}")
    return 0


def cmd_build(args):
    require_editor_closed()
    run = new_run_dir("build")
    log = run / "editor.log"
    unity_args = ["-quit", "-executeMethod", BUILD_METHOD]
    if args.output:
        unity_args += ["-buildOutput", args.output]
    if args.development:
        unity_args.append("-buildDevelopment")
    start = time.time()
    code = launch_unity(unity_args, log).wait()
    text = log.read_text(encoding="utf-8", errors="replace")
    summary = re.search(r"\[CommandLineBuild\] (\w+): (.+?\.exe), (.*)", text)
    print(f"Unity exited with code {code} after {time.time() - start:.0f}s. Log: {log}")
    if not summary or summary.group(1) != "Succeeded" or code != 0:
        print(summary.group(0) if summary else "No build summary in the log.")
        print_log_analysis(log)
        return 1
    exe = (PROJECT / summary.group(2)).resolve()
    print(f"Build succeeded: {exe} ({summary.group(3)})")
    if args.zip:
        zip_build(exe.parent)
    return 0


def zip_build(folder):
    folder = as_path(folder).resolve()
    archive = folder.parent / f"{folder.name}.zip"
    if archive.exists():
        archive.unlink()
    # Unity marks the folders that must not be shipped (Burst debug info): *_DoNotShip before Unity 6.6,
    # *_BackUpThisFolder_ButDontShipItWithYourGame since.
    subprocess.run([r"C:\Windows\System32\tar.exe", "-a", "-c", "-f", str(archive), "--exclude", "*DoNotShip*",
                    "--exclude", "*DontShip*", "-C", str(folder.parent), folder.name], check=True)
    print(f"Zip: {archive} ({archive.stat().st_size // (1024 * 1024)} MB)")


def cmd_zip(args):
    zip_build(args.folder)
    return 0


def cmd_screenshots(args):
    """The menu scenario renders each screen to Logs/screenshots (ScenarioDriver.CaptureScreen): run it, list them."""
    start = time.time()
    code = cmd_test(argparse.Namespace(platform="PlayMode", filter=args.filter, name="screenshots", detach=False))
    shots = sorted(p for p in (PROJECT / "Logs" / "screenshots").glob("*.png") if p.stat().st_mtime >= start)
    print(f"{len(shots)} screenshot(s), 1920x1080 (read them to check the screens):" if shots else "No screenshot taken.")
    for shot in shots:
        print(f"  {shot}")
    return code if shots else 1


def cmd_run_build(args):
    exe = as_path(args.exe).resolve()
    if args.seconds is None:
        # The smoke test stops itself after 20 min (DemoSmokeTest.k_MaxDuration): killing it sooner would fail it.
        args.seconds = 1200 if args.smoke else 20
    run = new_run_dir("player")
    log = run / "player.log"
    # Default: a small window. Only a windowed player runs its render loop, so only it reports render pipeline
    # errors (in -batchmode, a build that shows a black screen still passes).
    if args.headless:
        player_args = ["-batchmode", "-nographics"]
    else:
        player_args = ["-screen-fullscreen", "0", "-screen-width", "960", "-screen-height", "540"]
    if args.smoke:
        # The game plays two tournaments by itself (DemoSmokeTest) and quits, with pictures in the run's folder.
        player_args = ["-screen-fullscreen", "0", "-screen-width", "1280", "-screen-height", "720", "-demo-smoke",
                       "-no-analytics", "-demo-smoke-captures", str(run / "captures")]
    # This machine's save may have said yes to the statistics: a control run sends nothing unless asked.
    if not args.analytics and "-no-analytics" not in player_args:
        player_args.append("-no-analytics")
    player_args = ["-logFile", str(log)] + player_args
    prefs = snapshot_player_prefs()
    smoke_failed = False
    process = subprocess.Popen([str(exe), *player_args], cwd=exe.parent)
    print(f"Running {exe.name} (PID {process.pid}) for {args.seconds}s. Log: {log}")
    try:
        code = process.wait(timeout=args.seconds)
        print(f"The player exited by itself with code {code}.")
        if args.smoke and code != 0:
            smoke_failed = True
    except subprocess.TimeoutExpired:
        subprocess.run(["taskkill", "/PID", str(process.pid), "/T", "/F"], capture_output=True)
        print("Stopped the player (forced: its log ends abruptly, that is not a crash).")
        smoke_failed = args.smoke
    time.sleep(1)
    if restore_player_prefs(prefs):
        print("Restored the game's preferences (registry) to their state before the run.")
    if not log.is_file():
        print("No player log was written.")
        return 1
    text = log.read_text(encoding="utf-8", errors="replace")
    states = re.findall(r"^\[FM\] (\S+) - (\S+) - ENTER", text, re.M)
    print(f"Last state reached: {states[-1][0]} -> {states[-1][1]}" if states else
          "No FlowMachine state reached: the game did not get to its menu.")
    for line in re.findall(r"^\[DemoSmoke\] Performance: .*$", text, re.M):
        print(line)
    has_errors = print_log_analysis(log, args.warnings)
    return 1 if has_errors or not states or smoke_failed else 0


# What OnlineGuestBot's exit codes say.
RELAY_EXIT_CODES = {
    0: "ok",
    1: "an error logged, no game played, or game scenes left in the menu",
    2: "took too long",
    3: "couldn't join the room",
    4: "couldn't open the room (services, Lobby, Relay: see the [Online] warnings)",
    5: "the online screen can't type the room's code",
    6: "the connection doesn't go through Relay",
    7: "no room code, or no guest, in time",
    8: "the game couldn't be launched",
    9: "the host lost the room, or the guest left it",
    10: "the room never finished closing",
}
# A copy failed: the other one would wait for it until its own timeouts, or leaves on its own in this time.
RELAY_GRACE_SECONDS = 30


def relay_game_results(text):
    """The moves and the scores a copy saw at its game's results, from its report."""
    match = re.search(r"results, moves ([-\d, ]+), scores ([-\d, ]+)$", text, re.M)
    if not match:
        return None, None
    return [int(value) for value in match.group(1).split(",")], [int(value) for value in match.group(2).split(",")]


def cmd_relay_test(args):
    """Two copies play a room with a code, without -online-direct (OnlineGuestBot). No window: what this checks is the
    network, a window out of focus would pause its copy before it reaches the room, and run-build --smoke checks the rendering."""
    exe = as_path(args.exe).resolve()
    if not exe.is_file():
        print(f"No build at {exe}.")
        return 1
    run = new_run_dir("relay")
    code_file = run / "room-code.txt"
    # Each copy also signs in with its own profile (a mutex gives it, OnlineConnection.InstanceProfile), whose tokens go
    # to these preferences: the copies come back as new anonymous players after the restore.
    prefs = snapshot_player_prefs()
    copies = {}
    for role in ("host", "guest"):
        log, report = run / f"{role}.log", run / f"{role}-report.txt"
        process = subprocess.Popen([str(exe), "-batchmode", "-nographics", "-no-analytics", f"-relay-test-{role}", str(code_file),
                                    "-online-test-guest-report", str(report), "-logFile", str(log)], cwd=exe.parent)
        copies[role] = (process, log, report)
        print(f"{role}: PID {process.pid}. Log: {log}")
    start = time.time()
    deadline = start + args.seconds
    print(f"Waiting for both to quit, {args.seconds}s at most.")
    while time.time() < deadline and any(process.poll() is None for process, _, _ in copies.values()):
        if any(process.poll() not in (None, 0) for process, _, _ in copies.values()):
            deadline = min(deadline, time.time() + RELAY_GRACE_SECONDS)
        time.sleep(0.5)

    success = True
    codes = {}
    for role, (process, _, _) in copies.items():
        codes[role] = process.poll()
        if codes[role] is None:
            subprocess.run(["taskkill", "/PID", str(process.pid), "/T", "/F"], capture_output=True)
            success = False
    time.sleep(1)
    if restore_player_prefs(prefs):
        print("Restored the game's preferences (registry) to their state before the run.")
    print(f"Over after {time.time() - start:.0f}s.")

    player_ids = {}
    results = {}
    for role, (_, log, report) in copies.items():
        code = codes[role]
        print(f"\n{role}: " + ("stopped (forced: time up, or the other copy failed)" if code is None
                               else f"exit code {code}, {RELAY_EXIT_CODES.get(code, 'unknown code')}"))
        success = success and code == 0
        report_text = report.read_text(encoding="utf-8", errors="replace") if report.is_file() else ""
        for line in report_text.splitlines():
            print(f"  {line}")
        results[role] = relay_game_results(report_text)
        if not log.is_file():
            print("  No player log was written.")
            success = False
            continue
        log_text = log.read_text(encoding="utf-8", errors="replace")
        player_ids[role] = re.findall(r"^\[Online\] Signed in as player (\S+)", log_text, re.M)
        for line in re.findall(r"^\[Online\] (?:Signed in|Room open|Joined the room).*$", log_text, re.M):
            print(f"  {line}")
        if print_log_analysis(log, include_warnings=True):
            success = False

    print()
    if set(player_ids.get("host", [])) & set(player_ids.get("guest", [])):
        print("FAILED: both copies signed in as the same player (authentication profiles).")
        success = False
    host_moves, host_scores = results["host"]
    guest_moves, guest_scores = results["guest"]
    if host_moves is None or guest_moves is None:
        print("FAILED: a copy didn't report its game's results.")
        success = False
    else:
        print(f"Host saw moves {host_moves}, scores {host_scores}; guest saw moves {guest_moves}, scores {guest_scores}.")
        if host_scores != guest_scores:
            print("FAILED: the scores differ.")
            success = False
        if len(host_moves) < 2 or host_moves[1] == 0:
            print("FAILED: the guest's buttons didn't move its robot on the host.")
            success = False
        if not guest_moves or guest_moves[0] == 0:
            print("FAILED: the host's robot didn't move on the guest.")
            success = False
    print(f"Relay test {'passed' if success else 'FAILED'}. Run folder: {run}")
    return 0 if success else 1


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="command", required=True)

    sub.add_parser("status", help="project, Unity version, editor open or not").set_defaults(func=cmd_status)

    p = sub.add_parser("quick-compile", help="compile the project's C# with Unity's compiler, without Unity")
    p.add_argument("--assemblies", help="comma-separated names, e.g. Assembly-CSharp,BeepBoop.Tests.PlayMode")
    p.add_argument("--all-warnings", action="store_true", help="also show warnings from packages")
    p.set_defaults(func=cmd_quick_compile)

    sub.add_parser("compile", help="full headless import and compile (editor must be closed)").set_defaults(func=cmd_compile)

    p = sub.add_parser("test", help="run tests headless (editor must be closed)")
    p.add_argument("--platform", default="PlayMode", choices=["PlayMode", "EditMode"])
    p.add_argument("--filter", help="test name, full name or regex (Unity -testFilter)")
    p.add_argument("--name", help="label for the run folder")
    p.add_argument("--detach", action="store_true", help="return immediately with the PID (for runs over 10 minutes)")
    p.set_defaults(func=cmd_test)

    p = sub.add_parser("wait", help="block until a process exits")
    p.add_argument("pid", type=int)
    p.add_argument("--cleanup-test-results", action="store_true", help="remove the Test Runner's stray TestResults.xml")
    p.set_defaults(func=cmd_wait)

    p = sub.add_parser("results", help="summarize a test results XML")
    p.add_argument("xml")
    p.add_argument("--log", help="the run's editor log, to list the games played")
    p.set_defaults(func=cmd_results)

    p = sub.add_parser("log-errors", help="distinct errors of a Unity editor or player log")
    p.add_argument("log")
    p.add_argument("--warnings", action="store_true")
    p.add_argument("--top", type=int, default=25)
    p.set_defaults(func=cmd_log_errors)

    p = sub.add_parser("log-flow", help="FlowMachine transitions and scenario steps of a log")
    p.add_argument("log")
    p.add_argument("--machine", help="only this flow machine (GameObject name), e.g. Orchestrator or FlowMachine")
    p.set_defaults(func=cmd_log_flow)

    p = sub.add_parser("build", help="Windows player build (editor must be closed)")
    p.add_argument("--development", action="store_true", help="development build, folder suffixed _Debug")
    p.add_argument("--output", help="output folder relative to the project (default Builds/BeepBoop_Win_<date>)")
    p.add_argument("--zip", action="store_true", help="also zip the build folder next to it")
    p.set_defaults(func=cmd_build)

    p = sub.add_parser("zip", help="zip a build folder next to it, without the do-not-ship folders")
    p.add_argument("folder")
    p.set_defaults(func=cmd_zip)

    p = sub.add_parser("run-build", help="launch a built player, then triage its log")
    p.add_argument("exe")
    p.add_argument("--seconds", type=int, help="default: 20, or 1200 with --smoke (the game gives up after 20 min)")
    p.add_argument("--headless", action="store_true",
                   help="no window (-batchmode -nographics): faster, but rendering is not checked at all")
    p.add_argument("--analytics", action="store_true",
                   help="let the player send GameAnalytics events (with a consent saved on this machine)")
    p.add_argument("--smoke", action="store_true",
                   help="the game plays two tournaments against bots by itself and quits (20 min at most)")
    p.add_argument("--warnings", action="store_true")
    p.set_defaults(func=cmd_run_build)

    p = sub.add_parser("relay-test", help="two copies of a build play a real room (Unity services, Relay) by themselves")
    p.add_argument("exe")
    p.add_argument("--seconds", type=int, default=600, help="stops both copies after this long")
    p.set_defaults(func=cmd_relay_test)

    p = sub.add_parser("screenshots", help="run the main menu scenario, then list its screenshots")
    p.add_argument("--filter", default="MainMenu_EveryScreen", help="scenario that captures (default: the menu test)")
    p.set_defaults(func=cmd_screenshots)

    args = parser.parse_args()
    sys.exit(args.func(args))


if __name__ == "__main__":
    main()
