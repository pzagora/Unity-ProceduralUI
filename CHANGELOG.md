# Changelog

## [1.1.0]

- Added folded-state summary badges to the section headers. Collapsed Shape, Gradient, Shadow, Interaction and Masking sections show right-aligned glyphs with tooltips for their enabled settings, and hide automatically when the inspector is too narrow to show them without overlapping the title.
- Gradient angle now rotates smoothly and without warping: the direction is a pure rotation (no rate change at 0/90/180/270°), and Linear and Corners gradients tessellate the mesh before baking so bands stay straight at diagonal angles. At diagonal angles the gradient no longer stretches exactly to the corners.
- Fixed Individual corner artifacts with large radii: filled shapes cover the full interior depth (no inner hole when a corner radius exceeds half the shorter side), and diagonal corner-radius sums are clamped so a large corner cannot poke through an opposite rounded corner. Large single and adjacent corners are preserved.
- Added a **Shadow** section that casts a blurred drop shadow for the image. It captures the image to a render texture, blurs it with a separable Gaussian, and draws it behind the content with configurable color, offset, size and spread, plus a **Sample Content Color** option that tints the shadow with the content's own colors. The shadow follows the content's movement and only re-captures when the content or size changes; it is driven by a managed, hidden component.
- Added a **Mask Children** toggle to the Masking section that adds or removes a standard uGUI `Mask` (hidden and managed) so the image can clip its children to its rounded shape, with an optional **Show Mask Graphic**.
- Split the former "Interaction & Masking" section into separate **Interaction** and **Masking** sections.

## [1.0.2]

- Organized runtime, editor and test files by responsibility while preserving asset GUIDs and public namespaces.
- Removed Resources loading and moved the material and sprite into dedicated asset folders, preserving their GUIDs.
- Added a bundled ScriptableObject with automatic preload registration and build validation for runtime-created images.

## [1.0.1]

- Fixed partial-width foldout header backgrounds while preserving inset arrows and full-width clickable headers.

## [1.0.0]

- Integrated optional inline gradients and a standalone UIGradient component using shared settings and rendering code.
- Added collapsible Shape, Gradient and Interaction & Masking sections with editor-session state.
- Kept gradient fields animatable through Unity clips while exposing an automatically invalidating runtime API.
- Preserved ReceiptOCR's gradient data while removing redundant components from Procedural Images.
- Replaced allocating gradient triangle splitting with reusable buffers, cached key stops and complete vertex interpolation.
- Added fixed-ramp transitions, bounded perceptual sampling and a geometry-preserving fallback at the UI vertex limit.
- Placed the creation command under UI (Canvas), alongside Unity's standard Image.
- Extracted Procedural UI Image into a UPM package with runtime, editor and test assemblies under the ProceduralUI namespace.
- Replaced modifier components with inline corner modes and conditional inspector controls.
- Restored standard Image controls, including Preserve Aspect, sprite mesh, native size, sliced/tiled modes, material and masking settings.
- Separated shape coordinates from sprite UVs and fitted the shape to aspect-preserved geometry.
- Removed continuous Editor rebuilds and redundant property dirty notifications.
- Guarded collapsed layouts and zero edge softness against invalid mesh data.
- Added RectMask2D softness and retained shared default materials.
