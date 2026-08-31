#import <React/RCTBridgeModule.h>
#import <React/RCTEventEmitter.h>

/// React Native module that owns the Unity-as-a-Library lifecycle on iOS.
///
/// Ownership boundary (docs/architecture/rn-unity-boundary.md):
///   RN JS (NativeUnityBridgeTransport)
///     <-> this module (QanivaUnityBridge)
///     <-> UnityFramework
///     <-> Unity C# SimulationBridgeController (GameObject "SimulationBridge")
///
/// UnityFramework is loaded purely at RUNTIME (NSBundle + objc messaging +
/// dlsym) — no compile- or link-time Unity dependency. When the framework is
/// not embedded, isUnityAvailable() resolves false and startUnity() rejects
/// with E_UNITY_UNAVAILABLE — nothing pretends to work.
@interface QanivaUnityBridge : RCTEventEmitter <RCTBridgeModule>

/// The single JS event carrying raw bridge-protocol JSON from Unity.
+ (NSString *)messageEventName;

/// Called by the C entry point Unity invokes (see QanivaBridge_SendToHost).
+ (void)forwardUnityMessageToJs:(NSString *)json;

@end
