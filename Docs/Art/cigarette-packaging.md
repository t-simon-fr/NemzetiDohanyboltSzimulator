# Cigarette packaging

Approved brands: Lucky Stroke, Marlbroke, Downhill, Davidon. Existing cigarette IDs remain cig_red, cig_blue and cig_gold; Davidon uses cig_davidon. Convenience IDs remain water, gum and beer. Their catalog indices move from 3-5 to 4-6; the scene upgrade migrates the serialized references.

## Assets

- Approved reference: `Docs/Art/approved-packaging-concept.png`.
- Production albedo atlas: `Assets/Game/Art/CigarettePacks/PackagingAtlas.png` (1024 x 1536; four panels in a 2 x 2 grid).
- Top left: Lucky Stroke; top right: Marlbroke; bottom left: Downhill; bottom right: Davidon.
- Editable Unity meshes and materials: `Assets/Game/Art/CigarettePacks/`.
- Reusable prefabs: `Assets/Game/Resources/CigarettePacks/`.
- Model preview: `Builds/PackagingPreview/cigarette-packs.png`.
- Actual in-game shelf capture: `Builds/PackagingPreview/shop-shelf.png`.

Each pack uses a beveled mesh, a printed front/back, solid paper edges and a separate lid seam. The four prefabs share one printed atlas/material. Visuals add no colliders: the existing product root controls pickup, placement and inventory. Delivery carton samples use the same models.

Apply/regenerate using **Nemzeti Dohánybolt > Apply Cigarette Packaging** in Edit mode. This updates the existing shop scene and can be rerun without duplicating slots or moving the counter. It creates 32 main-rack slots and seven warehouse docks; Davidon shares an upper level of the existing warehouse rack. The optional nine-slot shelf accepts the four brands. Existing room and counter transforms are retained.

Catalog prices: 2200, 2300, 2400 and 2600 Ft. The first day starts with eight packs per brand. New drinks are a subsequent art pass; this change implements cigarette packaging.

## Validation

Unity Play-mode checks: CigarettePackagingChecks.Run (50); CompactShopChecks.BaseDay (107); CompactShopChecks.ExpandedDay (134); WarehouseChecks.InventoryAndTrash (36); ComputerWorkflowChecks.Run (55, including visibility and mouse-ray access for all seven order controls). Total 382 passed, zero failed. Unity console: zero errors/warnings. Scene upgrade run twice to check idempotency and counter position. Preview and shelf images rendered with Unity materials. No new WebGL build/deployment is included in this change.

## Image generation provenance

Mode: built-in image_gen tool. The atlas is generated from the approved concept, then used as the production material's albedo without image editing. Side colors, meshes, UVs and prefabs are constructed by the Unity editor generator. Assets are saved in the project, not referenced from the generation cache.

Final atlas prompt:

Create ONE production texture atlas for four fictional cigarette pack front panels for the approved Hungarian simulator. Reference image is the approved design reference, not an edit target. The output must be entirely flat, perfectly front-on 2D printed artwork, no perspective, no physical boxes, no bevel, no lighting, no shadows, no scene, no captions. Portrait image in 2:3 proportion, EXACT 2-by-2 equally sized grid covering the entire image edge to edge. Four front panel rectangles meet precisely at image center with NO spacing, NO gutters, NO outer margin. Each panel is the complete pack front with its art extending to its panel boundaries. TOP LEFT: exact name 'Lucky Stroke', cream paper background, large red circular roundel with black retro serif 'Lucky' over 'Stroke', fine red rules near top and above warning. TOP RIGHT: exact name 'Marlbroke', red top third, cream white lower body with angular white upward rooftop and a fine jagged dark crack through red, bold condensed black serif word 'Marlbroke' clearly centered above warning. BOTTOM LEFT: exact name 'Downhill', deep burgundy background, gold serif brand across upper middle, descending three-tier gold arrow motif centered underneath, fine gold lines. BOTTOM RIGHT: exact name 'Davidon', near-black background, elegant gold serif lettering across upper middle, small gold power-button emblem centered below, fine gold rules. On every panel at bottom place the same framed white rectangular health warning taking 23 percent of panel height, exact black Hungarian lettering 'A dohányzás káros\naz egészségre.' in two readable centered lines. Keep all important artwork within panel avoiding edge cuts. Crisp print graphics, very subtle paper grain only, no advertising extra slogans, no real-world brand names. Faithfully match reference designs, but flatten into usable diffuse albedo atlas. Readable text exact spelling. Four equally sized panels only, two columns two rows.
