# Android build result

## Latest product UI test APK

Built successfully on 14 October 2026 with Unity 6000.3.26f1, IL2CPP and ARM64 Android support after owner-approved browser sign-in.

- Actual source commit built: `62119ce0715ae85575df682287d354a573e643aa`.
- Successful workflow: https://github.com/vinovsh/habit-garden-app/actions/runs/38579214414
- Download: https://github.com/vinovsh/habit-garden-app/actions/runs/38579214414/artifacts/11905075627
- APK: `HabitGarden-test.apk`, 36,183,878 bytes, ARM64 development build.
- APK SHA-256: `870c2915aebe2ba7f6e2eb5b333b756f91958406ffae461dea27330535ec93f3`.
- Uploaded ZIP SHA-256: `9c4781188f547227c1eedd9c3853d55035d9ee6bd8ca37cf30d9fc3ea51765ea`.
- Unity C# compilation, font/resource generation, IL2CPP and Android packaging succeeded. APK ZIP integrity was verified in the build runner before upload.
- The workflow initially checked out an earlier commit, then fetched the source fix and built the actual commit listed above on attempt 2. The run's initial head SHA is therefore not the APK's source SHA.
- Independent source CI passed 20 domain tests and parsed 22 C# files, with complete metadata coverage.
- Full Unity EditMode execution and physical-device UI, notification, backup and performance testing remain pending. The build proves compilation and packaging, not device behavior.
- Temporary Unity sign-in was removed when the job finished. No production ad IDs or release signing were configured.

The APK includes the new Garden, Habits, Shop, Profile and guide screens, plant selection, vector illustrations, purchase previews, weekly totals and bundled TMP font. Extract the APK from the downloaded artifact ZIP and install it on an ARM64 Android test device.

## Previous baseline build

Built successfully on 2026-10-14 using Unity 6000.3.26f1 and the official Unity CLI, with owner-approved Unity Personal activation and Android module agreements. No paid plan or purchase was used.

- Source commit: `8bb74187d6f079189ab3f15c6729534ab619cb58`
- Successful workflow: https://github.com/vinovsh/habit-garden-app/actions/runs/38524173564
- APK: `HabitGarden-test.apk`, 34,841,930 bytes, ARM64, development build
- SHA-256: `32836870ed3302f9ff96d413b7da11959b94ab7aecbfc0d3e26d40d760498ec2`
- Downloaded artifact digest and APK ZIP integrity verified.
- Unity C# compilation and IL2CPP Android packaging succeeded.
- Existing separate CI run verified 16 domain tests. This browser-authenticated run did not execute the Unity EditMode test suite.
- No Android device runtime or visual verification has been performed. Live ads remain unconfigured.
- Compiler warnings: unused AdService.Error event and obsolete PlayerSettings.SetPropertyInt API.

This is the first installable test build, not a release-ready application. Remaining work is listed in STATUS.md.

