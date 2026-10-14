# Product screen implementation

This change implements the screen layout and flows in sections 2, 4, 11 and 12 of TECHNICAL_SPECIFICATION.md on the existing Unity app. It keeps offline saves, the coin ledger, reminder adapter, backup bridge and optional ad service.

## Review in Unity

1. Open with the pinned Unity 6000.3.26f1 editor and resolve packages. TextMeshPro is supplied by com.unity.ugui 2.0.0; Runtime and Editor assemblies explicitly reference Unity.TextMeshPro.
2. Run **Habit Garden → Prepare project**. It generates the local TMP font/atlas, render pipeline and Bootstrap scene.
3. Open Bootstrap and Play. The guide offers an empty garden or three example habits.
4. In Game view, inspect portrait aspect ratios such as 360 × 640, 390 × 844 and 412 × 915. All long screens scroll; the live garden stays in its own card.
5. Run EditMode tests. GardenPresentationTests verifies weekly local-date counts, corrections, month boundaries, empty statistics, growth stage labels and category snapshot isolation.

## Interaction review

- Create a habit with a category, weekdays, target and plant. Try an empty title, target 0, no days and a malformed reminder. An error should preserve entered values.
- Numeric targets must reach the target before the completion chime, 10 coins and one growth day are granted. Undo reverses that entitlement; navigation stays on Habits and scroll position is retained.
- Today excludes paused and unscheduled habits. All and Paused expose their respective lists. Archive retains history and the plant.
- In Garden, select a plant by tapping it or its text card. Its linked habit, stage and progress are shown. Plants outside the selected plot must not be selected.
- Shop opens an original illustration preview. Owned items and insufficient balances disable Buy. Optional ads remain hidden when not ready and reward exactly 30 coins through the existing adapter.
- Profile's seven-day bars count completed daily records, including zero days. Choose only owned pots. Plant another is offered only on the latest mature instance after its maturity day.
- Reduced motion and sound preserve their values when opening the guide. Restore and reset require confirmation. Android Back closes dialogs or returns from a form to Habits.

## Implementation boundaries

The UI has no cloud login. Habit categories are an additive optional field with a Personal fallback for old saves; targets and schedules keep their existing next-day semantics. Rendering does not grant rewards. Vector illustrations are original local geometry, not downloaded artwork. Ad/privacy and notification behavior use the existing adapters.

This environment has no Unity editor, Android SDK or .NET runtime, and its network proxy did not permit dependency installation. Local metadata, JSON, shell and whitespace checks are available. The PR's existing source-validation workflow can run C# parsing and domain tests. A green source workflow does not establish Unity API compatibility or an APK.

Outstanding spec acceptance work includes physical-device rendering and font review, 150% system font scaling, a TalkBack bridge, structured garden placement, replayable maturity celebrations, performance profiling and production ad integration. Those features are not represented as completed by this UI update.
