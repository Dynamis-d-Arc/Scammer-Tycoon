# Archived Soft 3D visual style

This earlier visual direction used rounded 3D models rendered to transparent sprites. The current game uses Pixel Office artwork; see PIXEL_ART_STYLE.md. The original assets and baker are retained for reference, and the city foreground mask is still used for traffic occlusion.

- Cream panels, sage controls, oak desks, rounded cream computers and visible keyboards.
- Seated workers with two typing/screen frames; four workstation rotations.
- Matching room props, 20 expansion-size room shells, city buildings, cars and boats.
- Full-screen office with on-demand management panels and illustrated shop cards.
- Existing saves and gameplay rules are unchanged.

Art source: Assets/Editor/SoftArtBaker.cs. Runtime composition: Assets/Scripts/Soft3DView.cs.
Generated textures: Assets/Resources/Soft3D.
Regenerate with SoftArtBaker.Bake in an isolated project copy, with graphics enabled; it creates a temporary empty scene.
Run ProjectSetup.VerifyAndBuild for gameplay, placement, navigation, art checks and the Mac build.
