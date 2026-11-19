# Unity Procedural UI

Rounded rectangles, capsules and outlines for Unity uGUI. `ProceduralUI.ProceduralImage` derives from `UnityEngine.UI.Image` and keeps corner configuration on the image itself.

## Installation

Requires Unity 6 and uGUI. Once version 1.0.0 is published, add `https://upm.zagora.dev` as a scoped registry with scope `com.zagoradev`, then install `com.zagoradev.unity.proceduralui` through Package Manager.

For local development, use **Add package from disk** and select this repository's `package.json`.

## Usage

Create **GameObject > UI (Canvas) > Procedural Image**. Select a corner mode in the Shape section:

| Mode | Settings |
| --- | --- |
| None | Square corners |
| Uniform | One radius for all corners |
| Individual | Top left, top right, bottom left and bottom right radii |
| Round | Automatic radius of half the shorter side |
| Edge | Radius and the edge whose two corners are rounded |

Border Width of zero fills the shape. Positive values draw an outline. Edge Softness controls the edge transition in local UI units.

Shape, Gradient and Interaction & Masking are collapsible. Their expanded state is retained during the editor session; collapsing a section does not disable its settings.

The inspector also exposes source sprite, tint, material, Simple/Sliced/Tiled/Filled image types, fill controls, Preserve Aspect, Use Sprite Mesh, Set Native Size, Raycast Target/Padding and Maskable. Preserve Aspect and sprite mesh controls follow standard Image applicability. Custom materials must implement the procedural vertex layout.

## Gradients

Enable the **Gradient** section on a Procedural Image. The inspector shows only the controls for the selected type:

| Type | Settings |
| --- | --- |
| Linear | Start and end colors |
| Corners | Four independent corner colors |
| Ramp | Unity gradient editor with independent color and alpha stops |

Angle zero runs from the bottom (start) to the top (end). Angle 90 runs from left to right. Coordinates follow the displayed image bounds, including Preserve Aspect, and remain independent of atlas UVs and fill amount.

Multiply combines the gradient with the Image tint and alpha. Override replaces them according to Intensity. CanvasGroup opacity and CanvasRenderer fading still apply after either mode.

Use **Add Component > UI > Effects > UI Gradient** for an ordinary uGUI Graphic. It uses the same renderer and settings. Procedural Images normally use their inline gradient without an additional component. TextMeshPro's own text rendering and vertex gradients are outside this component's scope.

## Runtime API

```csharp
using ProceduralUI;

image.CornerMode = CornerMode.Uniform;
image.UniformRadius = 16f;
image.BorderWidth = 2f;
image.preserveAspect = true;

image.Gradient.Enabled = true;
image.Gradient.Type = GradientType.Linear;
image.Gradient.StartColor = Color.blue;
image.Gradient.EndColor = Color.white;
image.Gradient.Angle = 90f;
```

`CornerRadii` uses clockwise order from the top left: X = top left, Y = top right, Z = bottom right, W = bottom left.

Gradient property changes dirty the mesh automatically. Use `image.Gradient.SetRamp(ramp)` to replace the color ramp; it copies the supplied keys so later mutations cannot silently bypass invalidation. `CopyRamp()` returns an editable copy. Copying or replacing a ramp allocates key arrays; normal rebuilds reuse the cached keys and mesh buffers.

The serialized settings use a struct so Unity animation clips can bind fields such as `gradient.angle`, `gradient.intensity` and `gradient.startColor.r`. The `image.Gradient` API is a stable wrapper over those settings and continues to access the current values after Undo or serialization changes.

## Rendering and performance

Images share a default material and sprite through the bundled `ProceduralImageAssets` asset. Editor registration adds it to Player Settings > Preloaded Assets and the build processor verifies it before building, preserving other preloaded assets. Unity loads its direct references for runtime-created images as well as scene and prefab instances. The package does not use Resources or require Addressables configuration. Shape data travels through vertex channels rather than per-image material instances. The component enables TexCoord1, TexCoord2 and TexCoord3 on its Canvas; these channels increase the vertex payload for that Canvas.

Geometry rebuilds use Unity's normal dirty/layout/animation lifecycle. There is no Update loop, Editor update subscription or modifier lookup. Runtime setters avoid redundant dirty notifications. Sprite UVs and shape coordinates are separate, so atlas subrects and partial fills do not relocate the corners.

UI masks, RectMask2D softness and ETC1 split-alpha sprites are supported. Like a standard Image, raycast testing is rectangular by default; procedural corners do not change the hit area. Pooling, batching and layout costs remain governed by uGUI. Use Sprite Mesh retains Unity's mesh-generation behavior, including its sprite-array allocations; the procedural encoding pass adds no managed allocations after warmup.

Gradients use vertex colors and the existing material, without a per-image material or gradient texture. Linear and corner modes add no geometry. Corner colors interpolate across the mesh triangles, as in the original UIGradient; they are not a per-pixel bilinear shader effect. Multi-stop ramps clip triangles into bands at the color and alpha stops, preserving the full UV, normal and tangent data. Fixed ramps keep sharp transitions. Perceptual Blend adds 32 sampling intervals to approximate its nonlinear interpolation.

Gradient rendering performs no Update polling, per-vertex trigonometry or LINQ. Rebuilds allocate no managed memory after buffer warmup for a stable mesh complexity. First use, a larger mesh and ramp-definition changes can allocate. Gradients increase CPU rebuild work and, for ramps, the vertex count; they do not make geometry work free. If splitting would exceed 64,998 vertices, the renderer preserves the original geometry and samples the gradient at its existing vertices. Complex gradients may consequently lose detail on exceptionally dense meshes.

## Migration from Procedural UI Image 2.2

The ReceiptOCR migration preserves the image, shader and default sprite GUIDs. Existing modifier values are transferred to the image before the separate modifier components are removed. Prefab instances continue to inherit the migrated settings.

Code now uses the `ProceduralUI` namespace and assembly. Replace `ModifierType` and modifier component access with `CornerMode`, `UniformRadius`, `CornerRadii` and `RoundedEdge`. Existing `BorderWidth` and `FalloffDistance` properties retain their names.

Animation clips and external projects targeting old modifier components require their bindings to be retargeted before removing those components.

The ReceiptOCR UIGradient migration moves eight gradients into their Procedural Images and retains one standalone gradient on an ordinary Image. Original colors, stops, blend modes, intensities, angles and enabled states are preserved. `UIGradient` now lives in `ProceduralUI`; configure it through its `Gradient` property. The old `Vector2Extension` helper is no longer required. External animation clips targeting the old component fields must be retargeted to the nested `gradient` fields.

## Package layout

- `Runtime/Images`: procedural image component, corner modes, geometry and shared resource access.
- `Runtime/Gradients`: gradient components, configuration and runtime API; `Rendering` contains mesh processing.
- `Runtime/Shaders`: the procedural image shader referenced by the shared material.
- `Runtime/Assets`, `Runtime/Materials` and `Runtime/Sprites`: the bundled asset references, shared material and default sprite.
- `Editor/Inspectors`, `Editor/Menus` and `Editor/Assets`: component inspectors, creation commands and automatic preload registration.
- `Tests/Editor`: tests grouped into Images, Gradients and Inspectors.

Assembly definitions remain at the Runtime, Editor and Tests/Editor roots. Folder organization does not change the public namespaces.

## Development

Requires Unity 6 and uGUI. Use this repository as a local UPM package while developing changes. Editor tests cover geometry, configuration retention, invalid dimensions, atlas coordinates and lifecycle behavior.
