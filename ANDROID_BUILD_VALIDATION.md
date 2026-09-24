# Android development build validation

Date: 24 September 2026. Source: `46d975d` plus the Android build setup committed with this record. Gameplay source matches that revision.

## Build result

- **Succeeded**, Unity 6000.3.13f1, isolated `.utmp/AndroidBuild` project.
- Entry point: `AndroidDevelopmentBuild.Run`, invoked with Android target in batch mode.
- Build duration reported by Unity: 20 minutes 25 seconds (initial project import excluded).
- Package: `com.kingdoms.prototype`, version 0.1.0/code 1, development/debug signing.
- ARM64, IL2CPP; min SDK 25, target/compile SDK 36.
- Scenes: WelcomeScene then Main Scene.
- Artifact: ignored `Builds/Android/Kingdoms-development.apk`, **75,881,750 bytes (72.37 MiB)**.
- SHA-256: `C37C360C8880A7992A50884B3A2ACD20D7A5BD1D96981B7F37195609158E32AF`.
- Unity report: **0 errors, 1,049 warnings**. Repeated installed inference-package compute-shader variant warnings and duplicate System.Runtime.CompilerServices.Unsafe versions appear in the log. A successful build does not establish those warnings have no runtime impact.

Unity's `summary.totalSize` reports 1,589,802,244 bytes for build outputs; this is not the APK file size above. Full local evidence is in ignored `Builds/Android/build-report.txt`, `unity-build.log` and `apk-verification.txt`.

## Checks run

`Tools/Verify-AndroidApk.ps1` passed against the actual APK using Unity's SDK build-tools 36.0.0 and OpenJDK:

- Expected package and ARM64 manifest ABI.
- ARM64 libunity.so and libil2cpp.so present; no ARMv7/x86 native library directory.
- APK signature verifies using v2 with an Android Debug certificate.
- `zipalign -c -P 16 4` passes. This checks ZIP alignment, not full ELF/device compatibility on 16-KiB-page hardware.
- Launchable activity: `com.unity3d.player.UnityPlayerGameActivity`.

Both PowerShell scripts passed parser checks. The Unity entry point and verification script were executed; the convenience build wrapper was syntax-checked but not separately executed end-to-end. `git diff --check` passed. Prior gameplay suites are recorded in TutorialValidation.txt and the linked feature records; they were not rerun for this build-only increment.

## Device gate remains open

Repeated `adb devices -l` checks returned an empty device list, including after packaging. No installation, launch, screenshot, physical touch, pause/resume, save-continuity, FPS or memory result is claimed. Follow [the device smoke test](ANDROID_BUILD.md#device-smoke-test) once the phone is available. This local development build is not a release-store package.
