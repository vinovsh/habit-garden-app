# Habit Garden

Unity Android app source for an offline habit tracker with a procedural 3D garden.

## Current status

Initial implementation, **not a verified release**. No APK has been built. The authoring environment has no Unity editor, Android SDK, NDK or Unity license. C# compilation, Unity EditMode tests, rendering and physical Android checks remain unexecuted. See `docs/STATUS.md` for all gaps.

## Run in Unity

1. Install Unity **6000.3.26f1** using Unity Hub, including Android Build Support, its SDK/NDK and OpenJDK. Activate your own eligible Unity license.
2. Add this directory as a project. Wait for package resolution and script compilation. The package versions in `Packages/manifest.json` are initial pins and have not been resolved here; commit Unity's generated package lock after a successful import.
3. Choose **Habit Garden → Prepare project**. This creates the URP asset, mobile settings and empty Bootstrap scene.
4. Open `Assets/Scenes/Bootstrap.unity`, then press Play. The runtime bootstrap creates the interface, garden and original procedural meshes.
5. Run Window → General → Test Runner → EditMode. Fix any import/API errors before proceeding to Android.

## Build a test APK

Choose **Habit Garden → Build test APK**, or:

```bash
export UNITY_EDITOR_PATH='/absolute/path/to/Unity'
bash Tools/build-android.sh
```

The script runs the Unity tests before building `Builds/HabitGarden-test.apk`. A failed or missing test report stops the build. The APK is a development build, not a signed publishing release. Verify the application ID belongs to your publisher before distributing.

## Implemented source

- Offline daily habits with numeric targets, selected weekdays, pause/archive, deferred edits and corrections.
- Local coin ledger, purchases, duplicate reward protection and growth linked to completed days.
- Checksummed JSON journal, checkpoints, damaged-save handling and backup validation.
- Garden, habit, shop, profile, guide and confirmation screens with safe area handling.
- Original generated pots, stems, branches, leaves, flower petals, decoration and completion audio.
- Three plant types with sequential organ growth, continuous leaf curl blend shapes and gentle sway.
- Android notification scheduling and a native Storage Access Framework backup picker.
- Conditional AdMob test reward adapter. Without the SDK, advertising stays disabled and no fake rewards are shown.

## Optional ad integration

Import the official Google Mobile Ads Unity plugin and resolve its Android dependencies. Configure the plugin's **test app ID**, then add the `HABIT_GARDEN_ADMOB` scripting define. The adapter uses Google's Android rewarded **test unit**, not revenue ads. Test initialization, consent, no-fill, earned/close callback ordering and duplicate callbacks on devices. Replace account IDs and add release gating only after a valid production account and consent configuration exist.

Ad creatives can be full screen, but the app only opens one after the user explicitly taps the shop reward button. No startup or habit completion ads are implemented.

## Repository layout

- `Assets/App/Domain`: engine independent rules and serializable records.
- `Assets/App/Runtime`: persistence, UI, plant generation, ads, audio and reminders.
- `Assets/App/Editor`: project setup and APK build entry point.
- `Assets/App/Tests/EditMode`: 24 authored Unity tests, not run here.
- `Assets/Plugins/Android`: portable JSON backup document picker.
- `Tools`: build and source validation scripts.
- `docs`: implementation status and requirements.

All meshes and audio in this prototype are generated from source; no user artwork is needed. They require visual refinement and phone profiling to reach the approved concept's quality.

## Original technical sources

- Unity release: https://unity.com/releases/editor/whats-new/6000.3.26f1
- URP: https://docs.unity3d.com/6000.3/Documentation/Manual/urp/urp-introduction.html
- Rewarded ads: https://developers.google.com/admob/unity/rewarded
- Consent: https://developers.google.com/admob/unity/privacy
- Android notifications: https://docs.unity3d.com/Packages/com.unity.mobile.notifications@2.5/manual/Android.html
