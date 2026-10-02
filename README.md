# Archarina64 Android test build

This public repository contains the buildable Android app source and a downloadable test APK. Development continues in a separate private working repository; this snapshot corresponds to the version 0.1.1 APK published under `android-port-latest` on 2026-10-02.

**Download:** [Archarina64 Android APK](https://github.com/ArtemisFlex/Archarina64-Android-Public/releases/download/android-port-latest/org.archarina64.android-Signed.apk)

The APK is a signed debug build for testing on Android 8.0 or later. It uses the original Archarina64 artwork for its launcher icon. It does not include a game ROM or extracted game assets. Use your own legally obtained ROM. The app is still a work in progress and does not yet cover every Sharp Ocarina feature.

To build from source on Windows, install .NET 10 with the Android workload and an Android SDK, then run `scripts/build.ps1`. The solution is `Archarina64.Android.sln`. The `desktop/` directory here contains only the three legacy source/resource files referenced by `src/Archarina64.Core/Archarina64.Core.csproj`.

License: GPL-3.0; see [LICENSE](LICENSE). The source snapshot used for this release comes from private development commit `f43c4f9`. The original launcher artwork is preserved in `desktop/Archarina64.ico`.
