# Habit Garden Technical Documentation

Android offline habits and interactive 3D plant growth

Version 1.0  |  14 October 2026  |  Implementation specification

Build Habit Garden as a Unity and C# Android application. Users complete daily habits to grow a persistent 3D garden. All habit tracking, plant growth, reminders and owned decorations work offline. Advertising is optional and confined to the shop reward flow when an ad is available.

This document defines the product rules, software architecture, local data model, complete asset production pipeline, realistic growth animation, monetization, performance targets, testing and release gates. It is intended for the developer and technical artist implementing the app. Proposed values are starting configuration targets, not measured results.

Visual reference from this conversation. The illustration guides art direction and screen layout; it is not a finished 3D asset or pixel exact implementation.

### Decisions already established

- Unity game engine with C# rather than React Native.

- Cute, colourful UI with natural looking growth and gentle plant motion.

- The implementation team creates every visual and audio asset; no user supplied art is required.

- No forced ads during habit creation, completion, navigation or growth.

## 1 Scope and development baseline

### First release

Android portrait app with Garden, Habits, Shop and Profile tabs. Include a short first launch guide, habit creation and editing, scheduled day selection, daily completion, statistics, local reminders, three plant species, garden placement, cosmetic purchases using coins, optional rewarded ads, local backup and restore, and settings for audio, graphics and reduced motion. No account or backend is required.

Start with sunflower, flowering tree and succulent. Support up to 20 active habits and 12 visible plants per garden plot. Mature plants remain in a collection; additional plots allow users to keep planting without removing past achievements. These limits are configurable and must be tested on the lowest supported phone.

| Component | Baseline decision |
| --- | --- |
| Editor | Unity 6.3 LTS; select and record a tested patch before implementation [1]. |
| Language and build | C#, IL2CPP, ARM64 Android release build. |
| Rendering | Universal Render Pipeline with a mobile forward renderer. |
| Interface | Unity uGUI and TextMeshPro; screen presenters separate from logic. |
| Content | Blender Python and Unity editor generators; bundled local assets. |
| Persistence | Versioned JSON snapshot plus transaction journal in app private storage. |
| Integration | Unity Mobile Notifications; Google Mobile Ads Unity plugin and UMP. |

### Version and device policy

Use Android 9 and 3 GB RAM as the proposed support floor, subject to the selected SDK requirements. Pin Unity, URP, notification, ad, consent and resolver versions in source control after a clean ARM64 build succeeds. Record JDK, Android SDK and NDK versions supplied by the selected Unity installation. Avoid untested preview packages.

The documentation defines implementation work; it does not imply that the app, assets or APK already exist. Final ad account identifiers, publishing credentials and signing ownership remain release inputs. No artwork needs to be supplied by the user.

## 2 User experience and screen requirements

### Garden

Show a fixed isometric garden camera, coin balance, local progress and streak summary. Tap a plant to see its linked habit, growth stage and progress. Provide a view habits action. Completing a habit in the list immediately updates its card; the garden shows a queued growth celebration when visible. Never navigate away from the list automatically to play an animation.

### Habits

Each card has an icon, title, target, current value and a completion control. Creation fields are title, category, selected weekdays, binary or numeric target, plant species and optional reminder time. Numeric habits support adding progress, such as one glass, without awarding completion until the target is reached. Title length is limited to 60 characters. Target values must be positive and within configured bounds.

Allow editing, pausing, archiving and same day correction. Confirm deletion of history separately from archiving a habit. Paused habits are excluded from due counts. Changes to targets or schedules take effect on the next local day to preserve today’s results; reminder changes apply immediately.

### Shop and profile

The shop shows owned status, coin price and preview before purchase. Purchases spend local coins transactionally. One optional reward card states the exact reward before an ad is shown. Profile contains weekly statistics, the mature plant collection, guide, reminders, backup, privacy choices, sound, reduced motion and graphics quality. A login icon must not imply a cloud account.

| UI requirement | Acceptance target |
| --- | --- |
| Touch and layout | At least 48 dp equivalent touch targets; safe areas and scrolling. |
| Typography | Readable text; localized labels and font scaling up to 150%. |
| Accessibility | Do not rely on colour alone; text labels and completion symbols. |
| Motion | Reduced motion toggle removes camera movement, wind and particles. |
| Ad states | Hide the reward card when unavailable; preserve shop layout smoothly. |

Use warm cream backgrounds, forest green text, sage buttons, peach and lavender accents. Cute 3D illustrations stay within their card bounds. Validate narrow displays and TalkBack navigation on devices; Unity canvas controls require explicit accessibility integration and testing.

## 3 Architecture and module boundaries

Use a persistent Bootstrap scene for services, a UI scene for navigation, and an additive Garden scene. The garden can unload while heavy integrations initialize. Domain rules remain plain C# with no dependency on GameObject, advertising or animation. Presenters transform domain state into UI state.

| Module | Responsibilities |
| --- | --- |
| HabitService | Create, edit, schedule, pause, archive and record daily progress. |
| DayService | Resolve local day and timezone; rollover and schedule evaluation. |
| GrowthService | Derive plant progress from eligible completed habit days. |
| WalletService | Apply ledger entries, purchases and idempotent reward grants. |
| SaveRepository | Serialize, validate, migrate and recover durable local state. |
| GardenPresenter | Load plants, place objects and reconcile growth visuals. |
| GrowthAnimator | Evaluate a plant pose and play visual transitions. |
| NotificationService | Manage channels, permissions and scheduled reminders. |
| AdService | Consent, load, show, reward callback and disposable ad objects. |
| AssetCatalog | Resolve stable content IDs to bundled prefabs and definitions. |

### Command flow

A presenter sends CompleteHabit with a command ID. HabitService validates the daily key, builds a transaction containing completion, earned coins and derived growth, and persists it before reporting success. A committed event then refreshes presenters. GrowthAnimator reacts to committed state; it cannot grant coins or modify habit records.

Contracts return structured results such as Success, InvalidTarget, AlreadyApplied or StorageUnavailable. Unity callbacks enter through a main thread dispatcher. Persistence and domain commands use a single writer queue; never allow concurrent writes from UI taps and ad callbacks.

### Interfaces and directories

Define IClock, ISaveRepository, IAdService, INotificationService and IAssetProvider so tests can supply fakes. Use Assets/App/Domain, Application, Infrastructure, Presentation, Garden, Content and Tests. Store source art and generator scripts in ArtSource. Commit Unity .meta files and package locks. Use assembly definitions to enforce dependencies from presentation toward domain, without circular references.

## 4 Habit rules and coin economy

### Daily completion and streaks

A daily record is uniquely identified by habit ID and local date. Binary habits complete with one explicit action; numeric habits complete when progress reaches the target. Only due, active habits contribute to daily totals. A habit streak counts consecutive scheduled occurrences completed, so a Monday Wednesday Friday habit is not broken on Tuesday. No due habits means no completion percentage, rather than a false 100%.

Store UTC time, local date and timezone offset with each event. Evaluate future days using the current device timezone; past daily keys remain unchanged. Reconcile rollover on launch, resume and while foregrounded. Never assume a 24 hour interval equals the next calendar day. Record the last observed clock; large backwards jumps suppress automatic rollover until the local date catches up or the user reviews a time change. An offline app cannot prevent all deliberate clock manipulation.

### Growth and maturity

Each habit owns one growing plant. One eligible completed scheduled day contributes one growth unit, regardless of numeric target size. Missed days pause growth; plants do not die. Completion thresholds are 14 units for sunflower, 30 for tree and 10 for succulent. On maturity, retain the plant and offer an explicit Plant another action, starting a new instance for the same habit. Never reset or replace the mature plant silently.

| Reward or cost | Initial value | Rule |
| --- | --- | --- |
| Habit completion | 10 coins | Once per eligible daily record. |
| Rewarded ad | 30 coins | Shop only; at most 3 grants per local day. |
| Leafy pot | 50 coins | Cosmetic only. |
| Rainbow planter | 80 coins | Cosmetic only. |
| Fence and butterfly | 70 and 60 coins | Cosmetic only. |

### Corrections and balance

Same day undo removes that day’s completion coins and growth contribution exactly once. Permit a negative adjustment balance if the coins were already spent; block new purchases until the balance is sufficient. Recompletion restores the same daily entitlement rather than creating additional rewards. Mature transitions are finalized at the end of the habit day, avoiding collection duplication during corrections. Never undo independent ad rewards when a habit changes.

Coins have no cash value, transfer or redemption. Ads do not contribute growth units. Values above are tunable design defaults, to be reviewed after playtesting.

## 5 Local data model and save integrity

| Record | Required fields |
| --- | --- |
| AppState | schemaVersion, revision, settings, habits, dailyRecords, plants, walletLedger, inventory, pendingVisuals, adSessions. |
| Habit | id, title, category, schedule, targetType, targetValue, speciesId, createdUtc, effectiveChanges, archived, paused. |
| DailyRecord | habitId, localDate, progress, complete, credited, completedUtc, timezoneOffsetMinutes. |
| PlantInstance | id, habitId, speciesId, seed, growthUnits, maturityDay, plotId, slotId, cosmeticIds, generatorVersion. |
| LedgerEntry | id, sourceType, sourceId, delta, createdUtc; unique source and operation key. |
| AdSession | id, placement, expectedReward, startedUtc, earned, granted, localDate. |
| Settings | sound, haptics, reducedMotion, quality, reminderPreferences, lastObservedDate, locale. |

### Write protocol

Maintain state.json, a previous valid snapshot and a checksummed write ahead journal. Each transaction has a unique ID and revision. Append and flush the complete journal entry before applying it to the in memory state; replay only complete checksum valid entries. Periodically write a new snapshot to a temporary file, flush it, validate it, and atomically replace the old snapshot on the same filesystem. Truncate the journal only after the snapshot replacement succeeds.

Persist successful completion, purchases and reward grants immediately. Do not rely on application quit callbacks, which may never run on Android. On startup validate schema, IDs, references and ledger consistency. Recover from the newest valid snapshot and replay newer journal revisions. Ignore a torn final journal entry. If recovery fails, offer restore or explicit reset; never silently discard a garden.

### Migration and backup

Migrations run sequentially from version N to N plus 1 against a copy. Retain the pre migration backup and refuse unknown future versions. Validate a full restore before replacing current state. Exports include schema, catalog version, app version and checksum. Use Android Storage Access Framework document create and open intents through a small native bridge, avoiding broad storage access permissions.

Save settings in the same repository; PlayerPrefs is unsuitable for transactional progress. Do not include ad consent SDK internals in portable backups. Local checksums detect corruption but do not make editable files tamper proof.

## 6 Plant growth animation design

Create botanically inspired motion in a stylized garden. A tree must grow from an anchored stem into a branching canopy; a flower must extend its stem, unfold leaves and open a bud. Scaling the entire finished plant from zero is not an acceptable primary growth effect. A full biological simulator is unnecessary; authored curves and deterministic plant geometry provide controlled mobile performance.

### Continuous pose evaluation

Growth is normalized from zero to one using earned units divided by maturity units, clamped to range. Evaluate the absolute pose from that value and a saved seed. Branches have parent IDs, attachment positions, birth progress and completion progress. Leaves and buds have independent emergence windows. A child begins only after its parent reaches the attachment point.

Use a swept spline mesh for stems and branches. Precompute topology in the editor. Blend shapes extend and thicken each segment while a capped tip prevents visible hollow stems. Leaves use petiole rotation plus same topology blend shapes for curl and unfurl. Blossoms use radial petal bones or morphs. Unity supports animating blend shape weights, but the meshes need compatible topology [2]. New organs are separate meshes revealed at near zero size and grown locally.

| Phase | Progress range | Visible behaviour |
| --- | --- | --- |
| Seed | 0 to 0.05 | Seed shell rests in soil; a root hint stays below the surface. |
| Sprout | 0.05 to 0.20 | Stem emerges and cotyledons open. |
| Young plant | 0.20 to 0.45 | Stem lengthens; first leaves unfurl. |
| Structure | 0.45 to 0.70 | Branches extend in parent order; canopy develops. |
| Bud | 0.70 to 0.88 | Buds emerge and swell; tree foliage thickens. |
| Mature | 0.88 to 1.00 | Flowers open or canopy reaches full volume. |

### Species differences

Sunflower grows one main stem with alternating leaves and a terminal blossom. Tree adds a trunk, ordered limbs, leaf clusters and small blossoms. Succulent expands a low rosette with staggered new leaves and thickening older leaves. These ranges are species configuration starting points; do not force the tree’s branching pattern onto every plant.

## 7 Animation implementation and natural motion

### Growth transition

On a committed growth event, animate from the displayed progress to the saved target in 2.5 to 4 seconds. Use eased temporal interpolation, with different organ curves so the stem leads, branches follow, and leaves unfurl last. A maturity celebration lasts up to 6 seconds and is skippable. Regular completion feedback remains short and never locks scrolling or tab navigation.

GrowthAnimator states are Idle, Transitioning, Settling and Suspended. If another target arrives, smoothly retarget from the current pose. Serialize the target and pending celebration ID before playback. Do not persist every animation frame. On restart, restore the saved final pose; offer a pending maturity replay once, without granting rewards again. Backgrounding stops animation and wind; resuming reconciles against durable state.

### Wind and interaction

Apply low frequency vertex displacement using height or vertex colour masks: trunk bases remain fixed, upper branches sway, and leaf tips flutter lightly. Vary phase by plant seed to avoid synchronized motion. Start with branch rotation under 2 degrees and leaf tip motion under 2% of plant height; tune by species. Avoid elastic stretching or moving the pot. Growth and wind are separate layers so the wind never changes growth progress.

A tap produces a restrained leaf sway and optional soft sound. Camera stays stable by default; permit limited drag rotation of the garden without rotating card controls. Do not use camera shake, rapid flashes or large zooms. Reduced motion snaps to the final growth pose with a brief crossfade and disables wind, particles and camera animation.

### Implementation outline

- Load PlantDefinition and deterministic branch descriptors for the saved seed.

- For each organ, map global progress into its emergence interval and evaluate a smooth growth curve.

- Apply length and thickness morphs, local orientation and leaf curl; resolve attachment transforms from the evaluated parent.

- Add bounded wind displacement without changing the saved pose.

- Emit visual completion once; domain state was already committed.

### Acceptance for realism

Check side by side renders at progress 0, 0.1, 0.25, 0.5, 0.75 and 1.0. Reject detached branches, inverted normals, leaf popping, disappearing geometry, obvious whole plant scaling, soil penetration above the pot, or hollow tips. Review a close camera view and normal garden view on a physical phone. Visual quality is established by review and profiling, not by the choice of engine alone.

## 8 Complete asset creation pipeline

The team creates original assets using reproducible generator scripts and manual refinement where needed. Blender Python generates branch meshes, rosettes, pots, fences, petal shapes and UV coordinates. Unity editor tooling generates PlantDefinitions, materials, animation bindings and prefabs. Use image generation for concept sheets and selected texture references; raster images are not rigged 3D meshes and cannot replace topology, rigging or animation work.

| Asset group | First release inventory | Production method |
| --- | --- | --- |
| Plants | 3 species with 6 growth landmarks each | Procedural geometry, rigs and same topology morphs. |
| Garden | Soil bed, ground, stones and background | Low polygon meshes and baked lighting. |
| Cosmetics | 4 pots, fence, butterfly, watering can | Generated meshes; shared colour palette. |
| Interface | Habit icons, tab icons, profile sprout | Original vector shapes and rendered 3D thumbnails. |
| Effects | Small sparkle, water drops, soft dust | Tiny atlas textures and pooled particle systems. |
| Audio | Tap, completion, growth, purchase and ambience | Original synthesis and layered generated sound. |

### Export and validation

Use metre units, applied transforms, root pivot at the soil surface, consistent forward direction and stable naming. Export FBX for rigged assets; retain .blend files and generation scripts. Verify blend shapes and vertex ordering after export. Use PNG for source textures and WAV for source sound. Generate LOD meshes, collision proxies and shop thumbnails in the editor. Runtime geometry is prebuilt except for lightweight pose evaluation.

Name assets by species and role, such as Tree_Trunk_Growth, Sunflower_Petal_Open and Pot_Leafy. Each catalog item has a stable ID, generator version, seed rules, material references and preview. Never use a display name as a database key. Existing plants retain their generator version after content updates.

### Provenance and availability

Track file name, source method, generator version, creation date and any third party license in an asset manifest. Fonts and SDKs are dependencies with their own licenses. Do not download unlicensed art or copied music. Bundle all required meshes, fonts, icons and sounds in the initial app so first launch works in airplane mode. Asset generation occurs during development, not on the user’s phone.

## 9 Garden rendering and performance

Use a small stylized diorama with a fixed camera, shared materials and a restrained lighting setup. Bake ground shading and static decoration lighting; use one main directional light for moving plants. Enable realtime shadows selectively on medium and high quality. Avoid HDRP, realtime global illumination, heavy depth of field and full scene physics.

| Budget | Low quality | Medium quality |
| --- | --- | --- |
| Garden frame rate | 30 fps sustained | 60 fps if thermal headroom permits |
| Visible triangles | Up to 80,000 | Up to 150,000 |
| Draw calls | Target below 100 | Target below 150 |
| Plant LOD0 triangles | Tree 8,000; flower 3,000 | Same assets; longer LOD range |
| Plant bone count | Tree under 48; flower under 16 | Same rig with fuller wind |
| Texture size | 512 to 1024 px atlases | Up to 2048 px shared atlas |
| Process memory | Target under 300 MB | Target under 400 MB |
| Installed content | Target under 180 MB | Measure engine and SDK overhead |

All budgets are profiling targets, not guarantees. Test the maximum 12 plant plot with ads initialized, shop thumbnails loaded and a growth celebration active. Static mature plants can use a baked mesh with shader wind; keep a growth rig only for plants actively transitioning. Skinned meshes require profiling and do not automatically receive the same batching benefits as static meshes.

### Quality and battery

Low quality uses no realtime shadows, reduced particles and simplified leaf wind. Medium adds soft shadows and modest anti aliasing. High can improve render scale while retaining the same garden population. Allow users to override automatic quality. Limit the garden to 30 fps when idle; hide or unload its rendering while a full screen habit list is shown. Pause audio, shaders and updates when backgrounded.

### Measurement

Use Unity Profiler, Frame Debugger and Android device captures. Profile CPU, GPU, allocations, texture memory and native memory on release equivalent builds. Target under 1 KB per frame managed allocation during idle garden motion. Run a 20 minute thermal test; if frame time degrades, lower shadows, render scale and wind density. Record cold launch and resume time; aim for an interactive offline home within 3 seconds on the target low tier phone.

## 10 Optional ads and monetization

The first release uses rewarded ads only. Display one optional reward card in Garden Shop when a showable ad is ready. The user taps Watch ad after seeing the reward amount. Ad SDK creative is typically a full screen experience; it occurs only after that explicit choice. No launch, completion, reminder, navigation or automatic growth ads are allowed. Core functions remain usable when offline, consent is unavailable or no ad is filled.

### Ad lifecycle and reward correctness

State machine: Disabled, Loading, Ready, Showing, Cooldown and Failed. Initialize the Google Mobile Ads SDK, load a rewarded object, verify CanShowAd, and grant only from the SDK reward callback [3]. A dismissal callback is not evidence that a reward was earned. Dispose used or failed ad objects and load replacements with bounded backoff. Network connectivity alone does not prove an ad is available.

Before showing, persist a unique AdSession ID with the expected placement and reward. Dispatch the earned callback to the main thread and commit its grant through WalletService using that ID as the unique source key. Duplicate callbacks never duplicate coins. If the app dies after a grant was journaled, replay restores it. If it dies before any earned callback is durably recorded, local only recovery cannot prove the reward; server verification would be a separate future feature. Never promise cross device reward recovery.

Pause garden audio during an ad and restore it after close or failure. SDK reward callback ordering can vary with mediation, so do not destroy pending session context solely because close fired. Keep callbacks bound to their session. Handle no fill and show failures quietly; never auto show a replacement.

### Consent and configuration

Use UMP to refresh consent information and present required messages; expose privacy choices when the SDK requires an entry point [4]. Request ads only when the SDK’s consent gate permits. Offline habits must not wait on this flow. Keep private habit titles out of ad targeting and logs. Begin with one network; defer mediation until the release is stable.

Use test ad units and test devices in development. Production requires the owner’s AdMob app and ad unit IDs, account setup and store configuration. Daily reward limits are local controls, not fraud protection. A banner or Remove Ads purchase is deferred; adding them requires updating this design, purchase restoration and testing. Earnings depend on usage and ad demand and are not guaranteed.

## 11 Reminders audio and offline behaviour

### Reminders

Use Unity Mobile Notifications to schedule local Android reminders [5]. Ask for notification permission when the user first enables a reminder, with a clear explanation. Permission denial does not disable the habit. Create a stable reminder channel and keep per habit notification IDs. Tap opens the corresponding habit through validated local intent data, without completing it automatically.

Prefer inexact daily reminders; this app does not need exact alarm access. Group overlapping reminders to avoid notification spam. Schedule a rolling window of upcoming due dates and refresh on launch, resume, schedule changes and timezone changes. Configure and test the package’s boot rescheduling behavior. OEM battery restrictions and force stop can delay reminders; settings should explain how to enable them without claiming guaranteed delivery.

Cancel upcoming reminders when the habit is archived or paused. Same day completion cancels that day’s pending reminder. Do not notify overnight unless the user explicitly chose that time. Reminder content can use a generic title for privacy, with an optional setting to show the habit name.

### Audio and touch feedback

Create a short soft tap, a gentle completion chime, leaf rustle for growth and a subtle purchase sound. Keep transients quiet and avoid clipping; target source peaks below minus 1 dBFS. AudioMixer groups are UI, Garden and Ambience, each independently muted. No background audio service is required. Haptics are optional, short and disabled with the device or app setting.

| Situation | Required behaviour |
| --- | --- |
| First launch offline | Show guide and create habits with all bundled content. |
| Offline completion | Persist progress and animate normally. |
| No ad fill | Hide the optional bonus card; purchases still use earned coins. |
| Storage write failure | Do not report completion success; preserve prior valid state. |
| Background or screen off | Stop garden rendering and sound; local reminders remain scheduled. |
| Restore backup offline | Validate and replace state without login or a server. |

## 12 Privacy accessibility and resilience

### Data handling

Habit titles, progress, garden and ledger stay in Android app private storage. Default to no cloud analytics. Diagnostic logs use event IDs and error codes, not habit text. Advertising SDKs can communicate over the network and have separate data practices; store declarations and the privacy policy must reflect the selected SDK configuration. Do not describe the entire app as collecting no data merely because habit storage is local.

Offer Delete all data with a confirmation and clear result. Reset habits, garden, wallet, notification schedules and exported file references. Previously exported backups remain under the user’s control and are not silently deleted. Do not store publishing secrets or private signing keys inside the APK or source repository.

### Accessibility

Label navigation controls and habit completion state for accessibility. Use at least 4.5 to 1 contrast for normal text as a design target and test the final palette. Provide a textual plant stage and progress indicator so the garden is understandable without animation. Verify focus order, TalkBack, device font size, narrow displays and screen safe areas. Garden placement must also have a list based accessible alternative.

### Resilience and known tradeoffs

| Risk | Mitigation |
| --- | --- |
| 3D app size and battery cost | Small diorama, bundled atlases, limited rigs, idle frame cap. |
| Generated art quality varies | Review every growth milestone and export; refine weak assets. |
| Local time and save manipulation | Consistent daily keys and ledger; accept offline limits. |
| Clock or timezone changes | Stable past records, current timezone for future schedules. |
| SDK integration breaks builds | Pin package versions; keep a clean release build gate. |
| Reminder delivery varies by phone | Inexact scheduling, boot tests and clear settings guidance. |

No unnecessary location, contacts, camera or microphone permissions. Internet is used for ads and consent; notifications are requested in context. Review SDK supplied manifest permissions in the final build. Requirements that depend on store rules or SDK versions must be checked again at release time.

## 13 Testing and acceptance criteria

Use Unity EditMode tests for domain and persistence, PlayMode tests for presentation and plant evaluation, and physical Android tests for rendering, notification and ad lifecycle behaviour. Test production equivalent IL2CPP builds, not only the editor. Inject a fake clock, ad service and repository for deterministic edge cases.

| Area | Release gate |
| --- | --- |
| Habit correctness | Duplicate tap, numeric progress, undo and recompletion produce one net daily entitlement. |
| Scheduling | Weekday gaps, paused habits, midnight, timezone shifts and clock rollback behave as specified. |
| Persistence | Kill during journal append, snapshot replacement and migration; recover the last committed state. |
| Purchases | Rapid double purchase, insufficient funds and restore preserve inventory and ledger invariants. |
| Ads | No fill, offline, consent blocked, duplicate earned callback, early close and app resume never force ads or duplicate coins. |
| Growth | All species pass landmark and transition review; reopening reproduces the same saved pose. |
| Notifications | Permission allow and deny, reboot, completion cancel, timezone change and force stop tested. |
| Accessibility | 150% text, TalkBack, reduced motion and narrow screen flows remain usable. |
| Performance | Worst case plot passes agreed frame, memory and thermal budgets on low and mid tier phones. |
| Offline | Cold launch in airplane mode permits habits, growth, shop spending, statistics and restore. |

### Device coverage

Cover the proposed support floor, Android 13 notification permission behavior, and a current Android version at release. Include at least one 3 GB low tier device, one mid tier device and one high refresh device. Test both selected Vulkan and OpenGL ES fallback configurations if shipping both; keep only validated renderer backends.

### Evidence and completion

Record build hash, Unity and package versions, phone model, OS, quality preset and result for each acceptance run. Capture short growth videos at normal and close view plus profiler evidence for a populated garden. Open issues must state severity and reproduction steps. A clean build alone does not satisfy animation or offline acceptance.

## 14 Implementation phases and build delivery

| Phase | Deliverable | Exit condition |
| --- | --- | --- |
| 1 Growth prototype | One tree, stem and leaf system on Android | Natural growth review and low tier frame budget pass. |
| 2 Offline foundation | Habit domain, ledger, saves and date rules | Recovery and duplicate command tests pass. |
| 3 Product screens | Garden, habits, shop, profile and guide | End to end offline journey and UI review pass. |
| 4 Full content | All species, pots, decoration and audio | Asset manifest and every growth milestone reviewed. |
| 5 Integrations | Reminders, backup and optional ads | Physical device notification and test ad cases pass. |
| 6 Release candidate | Signed test APK and store AAB | All acceptance gates and release checks pass. |

### Build process

Use Unity batch mode to run domain tests and build Android artifacts from a clean checkout. Pin the editor version in ProjectVersion.txt, packages in manifest and lock files, and record generator inputs. Regenerate derived assets deterministically before release when source art changes. Separate debug test ad configuration from production configuration. Keep logs free of private data.

Generate an APK for direct device testing and an Android App Bundle for Google Play. Use a stable application ID such as an owner chosen reverse domain name; record version code and version name. Use the target API, native library alignment and signing configuration required by the store at submission, verified against current official guidance rather than hardcoding a future rule here.

### Required release inputs

The owner supplies or configures publishing identity, production advertising account IDs, privacy policy hosting, signing ownership and store listing. These are account configuration inputs, not artwork. Keep secrets outside version control. Review Unity licensing and all SDK licenses against the publisher’s actual use and revenue before distribution; this document does not assert a licensing entitlement.

### Definition of delivered

Deliver the Unity source project, all original art and sound sources, reproducible generator scripts, asset manifest, package lock files, build instructions, test report, Android APK and AAB when implementation is complete. Include the original UI reference and the guide text. No mandatory remote asset download or backend is allowed for first launch. This request delivers the specification only; implementation and APK generation are separate work.

## 15 Configuration contracts and examples

### Plant definition

PlantDefinition is a ScriptableObject keyed by speciesId. Required fields are maturityUnits, generatorVersion, bundled prefab reference, growth landmarks, organ descriptors, wind profile, LOD thresholds, soil anchor, preview and audio profile. Organ descriptors carry parentId, attachPoint, birthProgress, fullProgress and animation binding IDs. Validate positive interval widths and parent order in the editor before a build.

### Application configuration

| Key | Default | Validation |
| --- | --- | --- |
| maxActiveHabits | 20 | Positive integer; UI and domain share the same limit. |
| plantsPerPlot | 12 | Placement cannot exceed capacity. |
| completionCoins | 10 | Nonnegative; frozen per daily entitlement. |
| adRewardCoins | 30 | Must agree with the displayed reward and placement configuration. |
| maxDailyAdGrants | 3 | Checked before show and during grant. |
| standardGrowthSeconds | 3 | Reduced motion uses a separate minimal transition. |
| defaultQuality | Auto | Select only validated Low, Medium or High presets. |

### Example completion transaction

Habit H1 is Read a book, with a 15 minute target and a sunflower. For local date 2026 10 14, progress reaches 15. Transaction T1 marks its daily record complete, adds ledger entitlement completion H1 2026 10 14 for 10 coins, raises the current plant’s earned units from 4 to 5, and queues visual event V1. T1 is journaled before the UI shows success. A repeated command returns the already committed result. Reopening loads five units even if V1 never finished playing.

### Example shop reward transaction

User taps an available 30 coin reward. Session A1 is saved before the SDK show call. An earned callback produces transaction T2 using source A1; a second callback sees the existing grant and returns without adding coins. Close clears presentation state and triggers a controlled preload; it does not grant anything. Coins appear only after T2 commits. The cosmetic shop remains available if the next ad fails to load.

## 16 Technical references and decision record

Official sources checked on 14 October 2026. Bracketed references support framework and SDK behavior. Gameplay values, architecture, asset counts and performance budgets in this document are proposed project decisions. Verify the exact selected package APIs and release requirements again when implementation begins.

### 1 Unity 6 releases and support

Supports the chosen 6.3 LTS baseline and its production support policy.

https://unity.com/releases/unity-6/support

### 2 Unity work with blend shapes

Describes imported morph animation and scripting access. Use the manual version matching the pinned editor.

https://docs.unity3d.com/6000.0/Documentation/Manual/BlendShapes.html

### 3 Google rewarded ads for Unity

Defines SDK initialization, rewarded loading, show readiness and earned callback handling.

https://developers.google.com/admob/unity/rewarded

### 4 Google UMP for Unity

Defines consent information refresh, required messages and privacy options flow.

https://developers.google.com/admob/unity/privacy

### 5 Unity Android notifications

Explains channels, scheduling, permissions and restart behavior. Pin and test a compatible package.

https://docs.unity3d.com/Packages/com.unity.mobile.notifications@2.4/manual/Android.html

### 6 Unity mobile optimization

General rendering optimization context; the numerical targets in this document are project budgets.

https://unity.com/resources/mobile-game-optimization-in-unity-6

### Decisions to validate in the first prototype

- Confirm tree growth feels natural at both close view and normal garden scale.

- Confirm the selected Unity patch, ad SDK and notification package build together.

- Measure actual memory, thermal behavior and launch time on a low tier phone.

- Tune growth thresholds and cosmetic prices after the first complete offline journey.

Keep a short decision log with date, chosen option, reason and test evidence whenever a baseline changes. Update this specification and acceptance tests together so the delivered app matches the documented behavior.
