# Pixel Office visual direction

Implemented from the user-supplied "02 / Pixel Office" reference. This replaces the previous Soft 3D presentation.

Art files: Assets/Resources/PixelOffice/{room,stations,typing,props,city,floor}.png.
Runtime: Assets/Scripts/Soft3DView.cs (legacy filename, now renders PixelOffice), RoomInteraction.cs, CityView.cs, DashboardUX.cs.
Import and visual checks: Assets/Editor/PixelArtImport.cs.

The runtime uses room_empty.png for architecture only. Sofa, shelves, coffee table, sideboard, rug, floor lamp, plants, moving boxes, waste bin and radiator are now individual RoomItems, alongside workstations, board, cooler, filing cabinet and phones. Every furnishing can be selected, moved and stored. Windows, doors, wall sconces and pipes remain architecture. furniture.png supplies the new sprites; FurnitureCatalog.cs contains explicit atlas rectangles and footprint definitions. Save data, economy, available floor area and placement grid are retained. Focus team follows a workstation without moving it; Fit office shows the owned floor.

The city uses the old geometric foreground mask only for traffic occlusion; none of the former city artwork is displayed.

## Art generation
Mode: built-in image_gen, not CLI. Files were copied into the project from the Codex generated_images folder. No runtime dependency on that folder.

Prompt set:
- stations.png: Production transparent game sprite atlas, exact 4 columns by 4 rows, no labels/dividers/background. Detailed isometric pixel-art wooden desks, drawers, beige CRTs, visible keyboards, mice, mugs, books, black swivel chairs. Rows: empty desktops; desktops with seated green-hoodie worker typing; empty laptops; laptops with seated purple-hoodie worker typing. Columns requested quarter-turn views. Match the supplied Pixel Office reference with warm amber highlights and navy shadows, no smooth clay 3D. Isolated whole sprites, consistent scale and transparent margins.
- typing.png: Create frame TWO of the workstation atlas. Preserve exact 1254x1254 canvas, 4x4 grid, sprite positions and identities. Change only occupied rows: hands shift two to three pixels while typing, forearms subtly change pose, screen content and cyan/green glow visibly change. Preserve transparent background and empty workstation cells.
- room.png: Detailed isometric cutaway basement office shell matching the attached Pixel Office reference. Transparent exterior, no desks, people, UI or readable text. Warm brown plank floor, brick/plaster walls, midnight-blue city windows, amber lamps, pinboards, posters, pipes. Back-wall bookshelves, trailing plants, green sofa, small rug/coffee table, water cooler. Keep center and front 75% clear for movable furniture. Thick brick foundation, rich handcrafted pixel art, no smooth 3D.
- props.png: Transparent 4-column by 3-row isometric pixel-art sprite atlas. Row1: standing corkboard, water cooler, filing cabinet with plant, table with three burner phones. Row2: potted plant, green sofa, bookshelves/vines, amber floor lamp. Row3: mustard car facing upper right, navy car facing lower right, two tugboat views. Reference-matching dark outlines, amber highlights and navy shadows. Final edit requested background/halo removal while preserving all object positions, sizes and the 1536x1024 canvas.
- city.png: Convert the previous city's exact camera, island silhouette, roads, waterfront, building footprints and heights into richly detailed nighttime pixel art matching the attached reference. Brick apartment towers, amber windows, HVAC, fire escapes, neon shops, trees, sidewalks, lamps, blue water reflections. No cars/boats, UI, labels or markers. Transparent exterior.
- floor.png: Single seamless 2:1 isometric diamond wood-floor tile matching room.png, transparent outside. Detailed amber/brown boards and subtle grain, no bevel, foundation, exterior shadow, furniture, walls or labels.

Generated dimensions were retained and atlas cells use normalized UV coordinates. Discarded generation attempts are not referenced by the project.

## Movable furniture conversion

Built-in image_gen produced `Assets/Resources/PixelOffice/room_empty.png` (1774×887) and `Assets/Resources/PixelOffice/furniture.png` (1254×1254). Original art remains available for reference; runtime no longer draws its embedded furniture. Exact prompts and reference paths are saved in `FURNITURE_ART_PROMPTS.json`. Atlas regions are sliced at runtime without modifying the generated alpha.
