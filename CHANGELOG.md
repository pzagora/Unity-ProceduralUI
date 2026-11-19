# Changelog

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
