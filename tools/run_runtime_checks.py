#!/usr/bin/env python3
"""Run C# and def regressions inside RimWorld 1.6 with a temporary mod/save directory."""
import argparse
from pathlib import Path
import os
import shutil
import subprocess
import tempfile

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--game-dir", required=True, type=Path)
parser.add_argument("--dotnet", default="dotnet")
args = parser.parse_args()
repo = Path(__file__).resolve().parents[1]
game = args.game_dir.resolve()
if not (game / "Version.txt").read_text(encoding="utf-8-sig").startswith("1.6."):
    raise SystemExit("Runtime checks target RimWorld 1.6.")
if not (game / "RimWorldLinux").exists():
    raise SystemExit("This isolated runtime runner currently supports the Linux player.")
managed = game / "RimWorldLinux_Data" / "Managed"
dotnet = Path(shutil.which(args.dotnet) or args.dotnet).resolve()
sdk_version = subprocess.check_output([str(dotnet), "--version"], text=True).strip()
compiler = dotnet.parent / "sdk" / sdk_version / "Roslyn" / "bincore" / "csc.dll"
runtime = Path(tempfile.mkdtemp(prefix="singularity-runtime-checks-"))
for name in ("RimWorldLinux_Data", "Data", "UnityPlayer.so", "steam_appid.txt"):
    (runtime / name).symlink_to(game / name)
shutil.copy2(game / "RimWorldLinux", runtime / "RimWorldLinux")
(runtime / "Mods").mkdir()
(runtime / "Mods" / "SingularityCore").symlink_to(repo)
test_mod = runtime / "Mods" / "RuntimeChecks"
(test_mod / "Assemblies").mkdir(parents=True)
(test_mod / "About").mkdir()
(test_mod / "About" / "About.xml").write_text("<ModMetaData><name>Singularity Runtime Checks</name><author>Vazyan</author><packageId>vazyan.singularitychecks</packageId><supportedVersions><li>1.6</li></supportedVersions></ModMetaData>")
references = [managed / name for name in ("mscorlib.dll", "System.dll", "System.Core.dll", "netstandard.dll", "Assembly-CSharp.dll", "UnityEngine.CoreModule.dll")]
references += [repo / "Assemblies" / name for name in ("VoidMinerProject.dll", "EccentricTools.dll")]
subprocess.run([str(dotnet), str(compiler), "-nologo", "-target:library", "-nostdlib", "-out:" + str(test_mod / "Assemblies" / "RuntimeChecks.dll"), *["-r:" + str(p) for p in references], str(repo / "tests" / "RuntimeChecks.cs")], check=True)
config = runtime / "SaveData" / "Config"
config.mkdir(parents=True)
mods = ["ludeon.rimworld", "ludeon.rimworld.royalty", "ludeon.rimworld.ideology", "ludeon.rimworld.biotech", "ludeon.rimworld.anomaly", "ludeon.rimworld.odyssey", "vazyan.singularitycore", "vazyan.singularitychecks"]
(config / "ModsConfig.xml").write_text("<ModsConfigData><version>1.6</version><activeMods>" + "".join("<li>" + name + "</li>" for name in mods) + "</activeMods></ModsConfigData>")
report = runtime / "results.txt"
env = dict(os.environ, SINGULARITY_TEST_REPORT=str(report))
print(f"Isolated runtime: {runtime}", flush=True)
try:
    subprocess.run([str(runtime / "RimWorldLinux"), "-batchmode", "-nographics", "-savedatafolder=" + str(runtime / "SaveData"), "-logfile", str(runtime / "Player.log")], env=env, stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT, timeout=180)
except subprocess.TimeoutExpired:
    raise SystemExit(f"Timed out; see {runtime / 'Player.log'}")
if not report.exists():
    raise SystemExit(f"No test report produced; see {runtime / 'Player.log'}")
print(report.read_text())
print("Headless mode validates defs/code, not rendering. Unity atlas/shader errors are expected with a null graphics device.")
raise SystemExit(1 if "FAIL " in report.read_text() else 0)
