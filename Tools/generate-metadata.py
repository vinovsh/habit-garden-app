"""Generate stable Unity GUIDs for original authored files. Preserve existing metadata."""
import hashlib
from pathlib import Path

root = Path(__file__).resolve().parent.parent
for path in sorted((root / "Assets").rglob("*")):
    if path.suffix == ".meta":
        continue
    meta = Path(str(path) + ".meta")
    if meta.exists():
        continue
    guid = hashlib.sha256(("habit-garden-v1:" + path.relative_to(root).as_posix()).encode()).hexdigest()[:32]
    text = "fileFormatVersion: 2\nguid: " + guid + "\n"
    if path.is_dir():
        text += "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    elif path.suffix == ".cs":
        text += "MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    elif path.suffix == ".asmdef":
        text += "AssemblyDefinitionImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    elif path.suffix == ".java":
        text += "PluginImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  iconMap: {}\n  executionOrder: {}\n  defineConstraints: []\n  isPreloaded: 0\n  isOverridable: 0\n  isExplicitlyReferenced: 0\n  validateReferences: 1\n  platformData:\n  - first:\n      Any: \n    second:\n      enabled: 0\n      settings: {}\n  - first:\n      Android: Android\n    second:\n      enabled: 1\n      settings: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    else:
        text += "DefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    meta.write_text("\n".join(line.rstrip() for line in text.splitlines()).rstrip() + "\n")
