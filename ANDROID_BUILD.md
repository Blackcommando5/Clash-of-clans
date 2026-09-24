# Android development build

This workflow creates a local-test APK from the current working tree using Unity 6000.3.13f1. It does not publish an app or use a release signing key.

## Build

Run from PowerShell:

```powershell
.\Tools\Build-Android.ps1
```

The script copies Assets, Packages and ProjectSettings into a fresh `.utmp/AndroidBuild-*` project, launches Unity hidden, and waits for the build. Your open editor and local village save remain separate. First import can take several minutes. The Unity installation must include Android Build Support, SDK/NDK and OpenJDK. Package and Gradle dependencies may need network access on the first build.

Outputs are kept in ignored `Builds/Android/`:

- `Kingdoms-development.apk`
- `build-report.txt`: build result, source revision, scenes, backend, architecture and size.
- `unity-build.log`: full Unity/Gradle diagnostics.

The APK uses `com.kingdoms.prototype`, version 0.1.0/code 1, IL2CPP, ARM64 and Android debug signing. WelcomeScene loads before Main Scene. Landscape orientation and the Input System follow the project settings. This package ID is separate from the previous Unity-template ID; Android stores their local data separately.

You can also switch the editor to Android and select **Kingdoms > Build Android Development APK**. The command-line script is preferable while another editor is open because it builds an isolated copy.

## Verify the package

```powershell
.\Tools\Verify-AndroidApk.ps1
```

This checks the manifest package ID, ARM64 Unity/IL2CPP libraries, signing and ZIP alignment, then writes `apk-verification.txt` beside the APK. It also records SHA-256. Package checks do not establish phone compatibility or frame rate.

## Device smoke test

Connect an ARM64 Android phone with USB debugging enabled and approve the computer on the phone. Confirm it appears as `device` in `adb devices -l` before installing. Use the `adb.exe` bundled in this Unity installation under `Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools`.

1. Install the APK using `adb install -r <apk-path>`. Do not uninstall or clear app data when testing save continuity.
2. Launch Kingdoms. Check WelcomeScene, name entry, village loading and landscape rotation.
3. Pan and pinch the home camera. Confirm buttons and safe-area margins are usable.
4. Open Village Guide; pause/resume hints, place a building, collect resources and prepare an army.
5. Scout and start a campaign attack. Select troop types, tap the green zone, use lane buttons and confirm dragging/multitouch does not deploy accidentally.
6. Finish or surrender, watch the replay, return home and resolve the result. Confirm currency is awarded once.
7. Background/resume the app and restart it. Confirm village, army, guide milestones and pending results survive.
8. Observe frame rate, touch response, temperature and memory during a full attack. Record the device/Android version and any failures before calling phone validation complete.

Build outputs, debug keys, local saves and isolated Unity caches must remain uncommitted. See [BEGINNER_GUIDE.md](BEGINNER_GUIDE.md) for the current gameplay walkthrough.
