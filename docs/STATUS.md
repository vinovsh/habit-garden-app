# Implementation status

This project is the first source implementation from the Habit Garden specification dated 14 October 2026. It is not yet a completed app or an APK.

## Work completed in source

Core daily entitlement rules, persistent state, journal recovery, shop economy, optional ad abstraction, three generated plant species, continuous leaf morphs, organ growth, a garden camera, four main screens, onboarding, simple progress counts, pot switching, owned decoration, local notifications, Android backup picker, and a test APK build entry point.

## Validation performed in this environment

Source inventory and JSON validation, C# and Java syntax parsing, shell syntax checks, deterministic Unity metadata generation, archive integrity and git checks. GitHub Actions compiled the actual domain C# and passed all 16 domain NUnit tests on 14 October 2026. Unity-specific API compilation, save/geometry tests and visual rendering remain unexecuted. See VALIDATION.md for the run evidence.

## Required before a usable test APK

1. Import and compile in the pinned Unity editor; resolve and lock package dependencies.
2. Run the authored domain, persistence and mesh tests. Resolve any failing API bindings or Unity behavior.
3. Run the app and validate UI placement on narrow portrait displays, font sizing, input keyboard and safe areas.
4. Review every plant at growth landmarks; refine geometry, shading, flower orientation and branch continuity. Procedural meshes are an initial artistic pass.
5. Build Android and test the Java backup bridge, notifications, IL2CPP and save recovery on actual phones.

## Remaining production features

- Detailed weekly charts and accessible garden placement controls.
- TalkBack integration and dynamic system font scaling; current UI uses built-in uGUI Text rather than the specification's TextMeshPro.
- A selection interface for plant interaction and structured garden rearrangement.
- Persisted maturity celebration replay and dedicated particles; current plant pose itself is restored deterministically.
- Historical schedule revisions for exact streak reconstruction after edits; current streak uses the habit's effective schedule.
- Runtime LOD, thermal adaptation, baked scene lighting and formal performance measurements. Meshes are generated on first launch in this prototype rather than baked in the editor.
- Additional audio, shop art thumbnails, premium polish and app icon.
- Verified AdMob/UMP SDK integration, production IDs, privacy declarations, consent state changes and bounded ad retry.
- Notification intent routing, timezone-change scheduling and reboot behavior verified on devices.
- Publishing signing, store AAB, release checks and release privacy policy.

## Infrastructure blockers

Unity and Android build tools are not installed in the authoring environment. No license is configured. Therefore there is no valid APK to deliver yet.

The owner created https://github.com/vinovsh/habit-garden-app, and the connected GitHub plugin can now upload the prepared source. The cloud workflow can compile and run 16 domain tests independently of Unity. Unity-specific tests, rendering and APK creation remain dependent on a valid Unity activation configuration; see CI_BUILD.md. Remote creation is no longer a blocker.
