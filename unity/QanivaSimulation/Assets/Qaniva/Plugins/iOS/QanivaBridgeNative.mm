// iOS native plugin compiled INTO UnityFramework by the Unity iOS export.
//
// Why this lives in the Unity project: Unity C# DllImport("__Internal") symbols
// must resolve inside UnityFramework at link time. The host app cannot provide
// them. Instead, the host registers a callback at runtime:
//
//   host (QanivaUnityBridge.mm)                UnityFramework (this file)
//   ---------------------------                ---------------------------
//   dlsym("QanivaRegisterHostHandler") ------> stores the function pointer
//   ...                                        _QanivaBridge_SendToHost(json)
//   hostHandler(json)  <---------------------- called by Unity C#
//                                              (NativeUnityBridge.SendToHost)
#import <Foundation/Foundation.h>

typedef void (*QanivaHostHandler)(const char *json);

static QanivaHostHandler sHostHandler = NULL;

extern "C" {

/// Called by the host app (via dlsym) after loading UnityFramework.
void QanivaRegisterHostHandler(QanivaHostHandler handler) {
  sHostHandler = handler;
}

/// Called by Unity C# (NativeUnityBridge, DllImport "__Internal").
void _QanivaBridge_SendToHost(const char *json) {
  if (sHostHandler != NULL) {
    sHostHandler(json);
  } else {
    NSLog(@"[QanivaBridgeNative] dropped Unity->host message (no handler registered): %s",
          json != NULL ? json : "(null)");
  }
}

}
