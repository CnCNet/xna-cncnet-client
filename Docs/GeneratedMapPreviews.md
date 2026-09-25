# Optional generated map previews

A map-preview button next to Favorite switches between Original (nearby PNG or embedded PreviewPack) and generated HD previews. Original is the initial mode and never launches the renderer, including when there is no original image. Selecting HD displays a cached result or generates one in the background. The current image remains visible while generating or after failure. The context menu can regenerate the selected HD preview explicitly.

## Mod configuration

Add `[MapPreviewRenderer]` to the active base `ClientDefinitions.ini`. With no executable or argument template, the integration is disabled. The executable is a trusted mod-provided external program; it is never chosen by a map.

```ini
[MapPreviewRenderer]
Executable=Resources/RenderedPreviews/REPreviewHost.exe
Arguments={game} {map} {output} {width} {height}
Width=1920
Height=880
TimeoutSeconds=300
AssetVersion=1
```

This example uses the separately distributed Red Eclipse adapter for CNCMaps. No renderer is bundled with the client or required for normal use. Configure the path and arguments for another renderer to match its CLI. It must create a PNG at `{output}` and exit zero. Placeholders `{game}`, `{map}`, and `{output}` expand to **already quoted** absolute paths; do not surround them with additional quotes. Width/height are integer hints, not mandatory output dimensions. The output path is a unique staging file inside `Client/MapPreviewCache`. The client does not invoke a shell.

Increase `AssetVersion` when changing mod artwork or renderer dependencies. Renderer executable size/time, invocation template, dimensions and this version invalidate generated entries. Maps use the existing `Map.SHA1`, shared with custom-map handling; identical map contents share one PNG regardless of file name.

## User settings

```ini
[Video]
RenderMapPreviews=yes
ShowGeneratedMapPreviews=no
```

The first setting is the **Allow generated map previews** checkbox in Display options. The second is saved by the map's HD/SD button. Disabling the checkbox uses the original preview and cancels any active renderer. Selecting Original also cancels active rendering. Small HD/SD button labels describe the mode to switch to; tooltips describe both actions. UI refreshes occur on the UI thread. Rendering, process waiting, validation and cache pruning run on tasks.

## Cache and lifetime

`Client/MapPreviewCache/<existing-map-SHA1>.png` and `.json` belong to the client. Original files are never overwritten. On map-list refresh/startup, orphaned hash entries are pruned. Custom-map deletion also removes its entry unless another loaded map with that hash still exists. Deleted or edited maps cannot publish stale in-flight results. Temporary renderer output is removed after success, failure, cancellation and timeout. No additional output files should be created outside the staging location.

One renderer runs at a time. Requests for the same map hash are coalesced. Starting a match cancels rendering; no job is published during a match. Nonzero exits, invalid images and timeouts are logged with the map path, while the original/current preview stays available. A killed client can leave staging files; only orphaned managed hash PNG/JSON entries are pruned automatically in this draft.

## Optional coordinate metadata

Standard renderers can omit metadata; the existing client waypoint projection remains in use. An isometric renderer with a different crop may write `{output}.transform.json`: `[cropX,cropY,cropWidth,cropHeight,scale,padX,padY,pngWidth,pngHeight,...waypointTriplets]`. Triplets are tile X/Y/height. Coordinates follow the client's 60x30 isometric grid. The client applies the transform only when displaying that generated image. This draft extension needs maintainer review for the external renderer contract.

## Validation

See `Tests/MapPreviewRenderer` for isolated process/cache tests. Human UI verification across themes and graphics backends remains necessary before merging. No gameplay or Phobos changes are included.

## Theme button images

Optional keys in `[MapPreviewRenderer]`:

```ini
HDButtonImage=previewHD.png
HDButtonHoverImage=previewHD_hover.png
SDButtonImage=previewSD.png
SDButtonHoverImage=previewSD_hover.png
```

Images use the normal theme asset lookup, like Favorite. HD represents the action to switch to generated previews; SD switches to Original. An idle PNG replaces the text and sets the button to its native pixel dimensions; 18x18 or 32x18 is recommended. Hover PNGs should match the corresponding idle size. Without a hover PNG, the idle image remains visible. Empty, missing or unreadable idle images fall back independently to SD/HD text on a transparent background. Missing/unreadable configured assets are logged. Restart the client after changing these settings or assets.

## Integration with existing image caching

`Map` retains its original immediate-PNG and non-immediate custom PreviewPack APIs. Resolving a preview produces a view-local `MapPreviewSource`: a completed generated PNG is immediate just like a nearby PNG. `MapLoader` loads both through the same texture-loading path. Missing/corrupt generated images fall back there, rather than in the control.

Embedded extraction still goes through the existing `MapPreviewCacheManager`: its sequential queue, LRU policy, cached-null results for hidden previews, and reference-counted image leases are unchanged. An external generation job remains asynchronous and separate from image extraction. It never runs inside a synchronous cache miss, and a not-yet-generated image is never inserted into the embedded-image cache as a null result.

`MapPreviewBox` requests the selected source from the loader and owns only its resulting texture. Generated-mode and projection metadata are not stored on shared Map instances. Projection data is captured for that view; calculating HD marker coordinates cannot overwrite the original preview's coordinate cache used by other views.

## Responsibilities

| Class | Responsibility |
| --- | --- |
| `IExternalMapPreviewExtractor` / `ExternalMapPreviewExtractor` | Asynchronously run the configured executable, capture bounded diagnostics, handle timeout and cancellation, and produce a PNG at a staging path. No UI or cache policy. |
| `MapPreviewRenderOptions` | Immutable configuration snapshot for one generation request. |
| `MapPreviewDiskCache` | Hash paths, validation, fingerprinting, staging cleanup, publishing PNG/metadata, and pruning. Receives an extractor through its constructor. |
| `MapPreviewGenerationService` | Queue and deduplicate requests, track map lifetime, cancel for game/settings changes, and notify views. Publication is guarded against map deletion. |
| `MapPreviewModeButton` | SD/HD selection, persisted preference and theme artwork. The host handles its usual settings-refresh event. |
| `MapPreviewSource` | Per-view selection and projection metadata, without shared map mutation or resource ownership. |
| Existing `MapLoader` / `MapPreviewCacheManager` | Load ready images and preserve the original embedded-image cache and lease lifecycle. |

The synchronous `IMapPreviewExtractor` returns an in-memory image and remains unchanged. External generation has its own asynchronous interface because it produces an output file, may take seconds, and needs cancellation. No synchronous adapter blocks the UI or the embedded-image cache worker. All new C# files explicitly enable nullable reference types.
