#import "QanivaUnityBridge.h"
#import <React/RCTLog.h>
#import <UIKit/UIKit.h>
#import <objc/message.h>
#include <dlfcn.h>
#include <mach-o/dyld.h>

// -----------------------------------------------------------------------------
// UnityFramework is loaded at RUNTIME from the app bundle (NSBundle + objc
// messaging). Deliberately no #import <UnityFramework/...> and no link-time
// dependency: the host app builds identically whether or not the Unity export
// exists, and QanivaUnityFramework (vendored pod) only handles embed+sign.
// If the framework is absent, isUnityAvailable() -> false and startUnity()
// rejects with E_UNITY_UNAVAILABLE — nothing fakes success.
// -----------------------------------------------------------------------------

static NSString *const kUnityMessageEvent = @"QanivaUnityMessage";

/// Unity C# GameObject + method the host sends into (created by
/// Qaniva.Bridge.BridgeBootstrap). Must match BridgeBootstrap.BridgeGameObjectName.
static NSString *const kUnityBridgeGameObject = @"SimulationBridge";
static NSString *const kUnityBridgeMethod = @"OnHostMessage";

static QanivaUnityBridge *sharedEmitter = nil;
static id sUnityFramework = nil; // UnityFramework* held as id
static BOOL sUnityRunning = NO;

static NSString *UnityFrameworkPath(void) {
  return [[NSBundle mainBundle].bundlePath
      stringByAppendingPathComponent:@"Frameworks/UnityFramework.framework"];
}

/// C callback handed to UnityFramework's QanivaRegisterHostHandler.
static void QanivaHostMessageHandler(const char *json) {
  NSString *message = json != NULL ? [NSString stringWithUTF8String:json] : @"";
  [QanivaUnityBridge forwardUnityMessageToJs:message];
}

static id LoadUnityFramework(void) {
  if (sUnityFramework != nil) {
    return sUnityFramework;
  }
  NSBundle *bundle = [NSBundle bundleWithPath:UnityFrameworkPath()];
  if (bundle == nil) {
    return nil;
  }
  if (![bundle isLoaded]) {
    [bundle load];
  }
  Class principal = bundle.principalClass; // UnityFramework
  if (principal == Nil) {
    return nil;
  }
  id ufw = ((id (*)(Class, SEL))objc_msgSend)(principal, NSSelectorFromString(@"getInstance"));
  if (ufw == nil) {
    return nil;
  }
  id appController = ((id (*)(id, SEL))objc_msgSend)(ufw, NSSelectorFromString(@"appController"));
  if (appController == nil) {
    // First load in this process: hand Unity the host executable's Mach-O header.
    // Resolved via dyld (image 0 = main executable) because &_mh_execute_header
    // cannot be linked from RN 0.76's app dylib.
    const struct mach_header *executeHeader = _dyld_get_image_header(0);
    ((void (*)(id, SEL, const void *))objc_msgSend)(
        ufw, NSSelectorFromString(@"setExecuteHeader:"), executeHeader);
  }
  ((void (*)(id, SEL, const char *))objc_msgSend)(
      ufw, NSSelectorFromString(@"setDataBundleId:"), "com.unity3d.framework");

  // Register the Unity->host message callback with the plugin compiled into
  // UnityFramework (unity/.../Assets/Qaniva/Plugins/iOS/QanivaBridgeNative.mm).
  typedef void (*HostHandler)(const char *);
  typedef void (*RegisterFn)(HostHandler);
  RegisterFn registerFn = (RegisterFn)dlsym(RTLD_DEFAULT, "QanivaRegisterHostHandler");
  if (registerFn != NULL) {
    registerFn(&QanivaHostMessageHandler);
  } else {
    RCTLogWarn(@"[QanivaUnityBridge] QanivaRegisterHostHandler not found in UnityFramework — "
               @"Unity->RN messages will be dropped. Is QanivaBridgeNative.mm in the Unity export?");
  }

  sUnityFramework = ufw;
  return ufw;
}

@implementation QanivaUnityBridge {
  BOOL _hasListeners;
}

RCT_EXPORT_MODULE(QanivaUnityBridge);

+ (BOOL)requiresMainQueueSetup {
  return YES;
}

- (dispatch_queue_t)methodQueue {
  return dispatch_get_main_queue();
}

- (NSArray<NSString *> *)supportedEvents {
  return @[ kUnityMessageEvent ];
}

+ (NSString *)messageEventName {
  return kUnityMessageEvent;
}

- (void)startObserving {
  _hasListeners = YES;
  sharedEmitter = self;
}

- (void)stopObserving {
  _hasListeners = NO;
}

+ (void)forwardUnityMessageToJs:(NSString *)json {
  dispatch_async(dispatch_get_main_queue(), ^{
    QanivaUnityBridge *emitter = sharedEmitter;
    if (emitter != nil && emitter->_hasListeners) {
      [emitter sendEventWithName:kUnityMessageEvent body:json];
    } else {
      RCTLogWarn(@"[QanivaUnityBridge] dropped Unity message (no JS listener): %@", json);
    }
  });
}

// --- exported JS API -----------------------------------------------------

RCT_EXPORT_METHOD(isUnityAvailable
                  : (RCTPromiseResolveBlock)resolve reject
                  : (RCTPromiseRejectBlock)reject) {
  BOOL present = [[NSFileManager defaultManager] fileExistsAtPath:UnityFrameworkPath()];
  resolve(@(present));
}

RCT_EXPORT_METHOD(startUnity
                  : (RCTPromiseResolveBlock)resolve reject
                  : (RCTPromiseRejectBlock)reject) {
  if (![[NSFileManager defaultManager] fileExistsAtPath:UnityFrameworkPath()]) {
    reject(@"E_UNITY_UNAVAILABLE",
           @"UnityFramework is not embedded in this build. Run scripts/export-unity-ios.sh "
           @"and reinstall pods.",
           nil);
    return;
  }
  id ufw = LoadUnityFramework();
  if (ufw == nil) {
    reject(@"E_UNITY_LOAD_FAILED", @"UnityFramework bundle could not be loaded", nil);
    return;
  }
  if (!sUnityRunning) {
    // Lifecycle decision (docs/architecture/rn-unity-boundary.md): initialise the
    // Unity runtime ONCE per process, then show/hide its window. Full
    // unloadApplication + re-run cycles are fragile on iOS.
    NSArray *arguments = [[NSProcessInfo processInfo] arguments];
    int argc = (int)arguments.count;
    char **argv = (char **)malloc(sizeof(char *) * (argc + 1));
    for (int i = 0; i < argc; i++) {
      argv[i] = strdup([arguments[i] UTF8String]);
    }
    argv[argc] = NULL;
    ((void (*)(id, SEL, int, char **, NSDictionary *))objc_msgSend)(
        ufw, NSSelectorFromString(@"runEmbeddedWithArgc:argv:appLaunchOpts:"), argc, argv, @{});
    sUnityRunning = YES;
  } else {
    ((void (*)(id, SEL, BOOL))objc_msgSend)(ufw, NSSelectorFromString(@"pause:"), NO);
    ((void (*)(id, SEL))objc_msgSend)(ufw, NSSelectorFromString(@"showUnityWindow"));
  }
  resolve(@YES);
}

RCT_EXPORT_METHOD(sendToUnity : (NSString *)json) {
  if (sUnityFramework == nil || !sUnityRunning) {
    RCTLogWarn(@"[QanivaUnityBridge] sendToUnity before startUnity — message dropped");
    return;
  }
  ((void (*)(id, SEL, const char *, const char *, const char *))objc_msgSend)(
      sUnityFramework, NSSelectorFromString(@"sendMessageToGOWithName:functionName:message:"),
      [kUnityBridgeGameObject UTF8String], [kUnityBridgeMethod UTF8String], [json UTF8String]);
}

RCT_EXPORT_METHOD(hideUnity) {
  if (sUnityFramework == nil || !sUnityRunning) {
    return;
  }
  id appController =
      ((id (*)(id, SEL))objc_msgSend)(sUnityFramework, NSSelectorFromString(@"appController"));
  UIWindow *unityWindow =
      ((UIWindow * (*)(id, SEL)) objc_msgSend)(appController, NSSelectorFromString(@"window"));
  UIWindow *rnWindow = nil;
  for (UIWindow *window in [UIApplication sharedApplication].windows) {
    if (window != unityWindow) {
      rnWindow = window;
      break;
    }
  }
  ((void (*)(id, SEL, BOOL))objc_msgSend)(sUnityFramework, NSSelectorFromString(@"pause:"), YES);
  [rnWindow makeKeyAndVisible];
}

RCT_EXPORT_METHOD(resumeUnity) {
  if (sUnityFramework == nil || !sUnityRunning) {
    return;
  }
  ((void (*)(id, SEL, BOOL))objc_msgSend)(sUnityFramework, NSSelectorFromString(@"pause:"), NO);
  ((void (*)(id, SEL))objc_msgSend)(sUnityFramework, NSSelectorFromString(@"showUnityWindow"));
}

@end

// Unity -> host messages arrive via QanivaHostMessageHandler, registered with
// the QanivaBridgeNative.mm plugin inside UnityFramework (see LoadUnityFramework).
// The DllImport("__Internal") symbol _QanivaBridge_SendToHost is defined THERE,
// not here — it must resolve inside UnityFramework at its link time.
