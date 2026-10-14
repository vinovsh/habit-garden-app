"""Parse source syntax and project metadata. This is not a Unity compilation check."""
import json
from pathlib import Path
import sys
import tree_sitter_c_sharp
import tree_sitter_java
from tree_sitter import Language, Parser

root = Path(__file__).resolve().parent.parent
parser = Parser(Language(tree_sitter_c_sharp.language()))
failures = []
sources = sorted(root.glob("Assets/**/*.cs"))
for path in sources:
    tree = parser.parse(path.read_bytes())
    if tree.root_node.has_error:
        failures.append(str(path.relative_to(root)))
java_parser = Parser(Language(tree_sitter_java.language()))
for path in root.glob("Assets/**/*.java"):
    if java_parser.parse(path.read_bytes()).root_node.has_error:
        failures.append(str(path.relative_to(root)))
for path in sorted(root.glob("Assets/**/*.asmdef")) + [root / "Packages/manifest.json"]:
    json.loads(path.read_text())
guids = {}
for path in root.glob("Assets/**/*.meta"):
    guid = next(line.split(": ", 1)[1] for line in path.read_text().splitlines() if line.startswith("guid: "))
    if guid in guids:
        failures.append("Duplicate metadata GUID: " + str(path))
    guids[guid] = path
for path in root.glob("Assets/**/*"):
    if path.suffix != ".meta" and not Path(str(path) + ".meta").exists():
        failures.append("Missing metadata: " + str(path.relative_to(root)))
tests = sum(path.read_text().count("[Test]") for path in root.glob("Assets/App/Tests/**/*.cs"))
print(json.dumps({"csharp_files": len(sources), "authored_tests": tests, "unique_asset_guids": len(guids), "failures": failures}, indent=2))
if failures:
    sys.exit(1)
