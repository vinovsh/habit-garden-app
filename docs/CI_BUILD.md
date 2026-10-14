# Android builds through GitHub Actions

The workflow at `.github/workflows/validate-and-build.yml` runs whenever `main` changes and can also be started using Actions → Validate and build Android → Run workflow.

## Checks that need no Unity account

The source job compiles the actual domain C# files under .NET 8 and runs the existing 16 NUnit habit and economy tests. It also parses Unity C# source, validates manifests and metadata, and checks shell syntax. These checks cannot validate Unity-specific APIs or plant rendering.

## Unity activation

The workflow checks whether `UNITY_LICENSE`, `UNITY_EMAIL` and `UNITY_PASSWORD` are configured as repository Actions secrets. It checks presence only, never prints values. Until they are available, the workflow records the missing activation setup and skips Unity tests and APK creation.

Configure the secrets directly in GitHub Settings → Secrets and variables → Actions following the current official GameCI activation instructions:

https://game.ci/docs/github/activation/

Do not send a password or license file through chat, put it in source control, or save it in workflow logs. If you already have a licensed Unity build host, `Tools/build-android.sh` can build there instead. A Unity Pro or floating-license setup needs the corresponding GameCI configuration rather than the Personal-license path used here.

## Build sequence

1. Compile and run the engine independent domain tests.
2. Run all 24 Unity EditMode tests with the pinned editor.
3. Generate the URP assets and Bootstrap scene, then build an ARM64 development APK.
4. Publish `HabitGarden-test-apk` as a downloadable Actions artifact only after the build succeeds.

The pinned Unity editor is 6000.3.26f1. If the corresponding GameCI Docker editor image is unavailable, use a supported patch after reviewing compatibility and update both ProjectVersion.txt and the workflow together.

Live revenue ads are not enabled in this initial build. The AdMob SDK and production account configuration remain separate integration work. This APK is for testing, not Play Store publication.
