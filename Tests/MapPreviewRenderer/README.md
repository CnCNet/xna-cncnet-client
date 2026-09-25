# Preview lifecycle tests (Windows .NET Framework 4.8)

Compile PreviewTests.cs with the a modern Roslyn C# compiler (C# 8 or later, for nullable annotations) targeting .NET Framework 4.8, referencing System.Drawing.dll. Run **only in a disposable fixture directory**, never the live game folder: the test intentionally creates/removes Maps/Standard/test.map and copy.map and writes UserPreviewTest.ini.

Copy a built WindowsDX net48 client and dependencies to the fixture root, plus a valid mod Resources directory containing ClientDefinitions.ini, DTACnCNetClient.ini and GameOptions.ini. Supply a test Maps/Standard/test.map. Add this to the fixture ClientDefinitions.ini:

```ini
[MapPreviewRenderer]
Executable=PreviewTests.exe
Arguments=--renderer {map} {output} {width} {height}
Width=128
Height=64
TimeoutSeconds=1
AssetVersion=test
```

The test executable doubles as a deterministic external renderer and validates lazy Original mode, disabled/missing configuration, SHA1 reuse, preservation of originals, failure/timeout, duplicate suppression, game-start cancellation, corrupt-cache replacement, invalidation, pruning and in-flight deletion. It runs without a graphics device. Renderer artwork correctness and GUI placement require separate integration testing.
