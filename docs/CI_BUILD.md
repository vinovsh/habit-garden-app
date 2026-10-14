# Android builds through GitHub Actions

Two workflows support this project. The standard validation workflow runs domain tests and source checks on pull requests; full GameCI tests and APK jobs need repository Unity secrets. The browser sign-in workflow provides a separate route that uses an owner-approved Unity sign-in link without placing a password in chat.

## Source checks

Validate and build Android compiles the domain C# under .NET 8, runs 20 domain/presentation tests, parses C#/Java, validates manifests and Unity metadata, and checks shell syntax. These checks do not establish Unity API compatibility or rendered UI correctness.

## Browser sign-in build

Run **Actions → Unity browser sign-in and Android build → Run workflow**, selecting `codex/habit-garden-ui` to build the new interface. The workflow also starts when its YAML changes.

1. Download the `unity-browser-approval-link` artifact promptly and follow the URL inside `unity-browser-link.txt`.
2. Sign in and approve directly with Unity. The link and CLI wait expire; no passwords or license files should be shared through chat or committed.
3. The workflow checks the existing owner-approved license agreement record in `.ci/unity-license-consent.json`. It does not authorize paid purchases.
4. It activates Unity Personal, installs the pinned Unity editor with Android support, runs `ProjectSetup.BuildApk`, and uploads `HabitGarden-test-apk` only on success.
5. Download the APK artifact from the run. Temporary CLI authentication is removed when the job finishes.

A browser build compiles the actual Unity project and performs IL2CPP packaging. It does not automatically execute the Unity EditMode suite or demonstrate device UI behavior. Keep source-test, build and physical-device evidence separate.

## Standard GameCI route

The workflow checks `UNITY_LICENSE`, `UNITY_EMAIL` and `UNITY_PASSWORD` repository Actions secrets, without printing values. Configure them directly in GitHub Settings following https://game.ci/docs/github/activation/ if using this route. Pro/floating licenses need the corresponding configuration. Without these secrets, the standard workflow skips Unity tests and APK creation; browser sign-in remains an independent option.

## Local route

Open the source with Unity 6000.3.26f1 plus Android Build Support, run **Habit Garden → Prepare project**, then Play or use `Tools/build-android.sh` on that licensed host. Prepare generates the TMP font/atlas, URP settings and Bootstrap scene.

Use development builds for device testing. Live ads, store signing and production release acceptance remain separate work.
