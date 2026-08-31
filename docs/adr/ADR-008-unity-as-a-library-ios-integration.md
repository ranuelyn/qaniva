# ADR-008 — Unity as a Library integration on iOS

## Status

Accepted (2026-08-31).

## Context

QAN-004 requires the real RN ↔ Unity transport. The mobile app was Expo CNG
(no native projects); Unity-as-a-Library needs native project surgery (embedding
UnityFramework, a native bridge module, window management) that would otherwise
have to be re-applied on every `expo prebuild`.

## Decision

1. **Bare-workflow ownership of `ios/`.** `expo prebuild --platform ios` was run
   once and the generated `ios/` project is committed (minus `Pods/`, `build/`,
   `.xcode.env.local`). Prebuild is no longer part of the normal workflow; native
   changes are made directly. (A config-plugin approach was rejected: it would
   have to replicate the whole UaaL integration to survive regeneration.)
2. **The native transport is a local CocoaPod** —
   `apps/mobile/modules/unity-host` (`QanivaUnityHost`) — added via the Podfile.
   No hand-editing of `project.pbxproj`.
3. **UnityFramework is loaded purely at runtime.** The module uses `NSBundle`
   loading + `objc_msgSend` + `dlsym`; it has **no compile- or link-time Unity
   dependency**. The app builds identically with or without the Unity export;
   without it, `isUnityAvailable()` is false and `startUnity()` rejects with
   `E_UNITY_UNAVAILABLE`.
4. **The framework is embedded by a wrapper pod** (`QanivaUnityFramework`,
   `apps/mobile/unity-frameworks/`) using `vendored_frameworks`, added by the
   Podfile **only when** `unity-frameworks/ios/UnityFramework.framework` exists.
   The framework binary is a build artifact (git-ignored), produced by
   `scripts/export-unity-ios.sh`.
5. **Unity→host messages use the register-handler pattern.** Unity C#
   `DllImport("__Internal")` symbols must resolve inside UnityFramework at its
   link time, so `_QanivaBridge_SendToHost` is defined by a plugin **inside the
   Unity project** (`Assets/Qaniva/Plugins/iOS/QanivaBridgeNative.mm`). The host
   registers a C callback via `dlsym("QanivaRegisterHostHandler")` after loading
   the framework.
6. **Initialise-once lifecycle.** The Unity runtime is started once per process
   (`runEmbeddedWithArgc`) and afterwards shown/hidden (`showUnityWindow` /
   `pause:` + re-keying the RN window). Full `unloadApplication` + relaunch
   cycles are historically fragile on iOS and are not used.
7. **Transport selection in JS is explicit.** `selectUnityTransport()` picks the
   native transport when the `QanivaUnityBridge` module exists; otherwise it
   returns the labelled `FakeUnityBridge` with a loud warning, and the UI shows a
   "FAKE BRIDGE" badge. The fake is never used silently.

## Alternatives considered

- **Expo config plugin for the whole UaaL setup** — rejected (see Context).
- **Compile-time `#import <UnityFramework/...>` + weak linking** — rejected: the
  pod would fail to compile/link until the export exists, and search-path plumbing
  is brittle across pod installs.
- **Committing the Unity iOS export** — rejected: hundreds of MB of generated
  artifacts; regeneration is scripted instead.
- **`react-native-unity` community packages** — rejected for the proof: another
  dependency layer over the same UaaL API, less control over the message contract.

## Consequences

- `pod install` output states loudly whether Unity is embedded.
- Android repeats this pattern later with `UnityPlayer` + a small Java plugin
  (backlog).
- The Unity export must be regenerated when Unity-side code changes
  (`scripts/export-unity-ios.sh`); stale-framework risk is called out in the
  script and docs.
