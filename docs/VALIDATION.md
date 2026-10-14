# Validation record

Date: 14 October 2026

| Check | Result |
| --- | --- |
| Tree-sitter parsing of 15 C# files | Passed with no syntax errors |
| JSON manifests and assembly definitions | Parsed successfully |
| Tree-sitter parsing of Android Java bridge | Passed with no syntax errors |
| Unity asset metadata | 28 unique GUIDs with all authored assets covered |
| Android build shell script syntax | Passed bash syntax check |
| Missing editor build guard | Correctly exits with code 2 and no APK |
| Domain C# compilation on GitHub Actions | Passed under .NET 8 |
| Domain NUnit tests on GitHub Actions | 16 passed, 0 failed, 0 skipped |
| All Unity EditMode tests | 24 authored tests; full Unity execution still pending |
| C# type checking and Unity import | Not run; Unity unavailable |
| Android Java compilation | Not run; Android SDK unavailable |
| Plant animation visual review | Not run; Unity renderer unavailable |
| APK generation | Not performed |

Verified cloud run: https://github.com/vinovsh/habit-garden-app/actions/runs/38512191268

Source commit tested: cf15b0ea6b64de38e68025e1436da2359bc2b139. The source job completed successfully. The configuration job found UNITY_LICENSE, UNITY_EMAIL and UNITY_PASSWORD empty. The Unity EditMode and APK jobs were therefore skipped. The green overall workflow result confirms source checks, not an Android build.

Source syntax parsing does not establish API compatibility or runtime correctness. The initial source pins Unity 6000.3.26f1 and URP 17.3, but they must be imported and tested together on a licensed Unity host. The project intentionally does not manufacture a package lock or claim successful engine import.

Python source parsing dependencies used here: `tree-sitter` and `tree-sitter-c-sharp`. They are verification tools, not mobile app dependencies.
