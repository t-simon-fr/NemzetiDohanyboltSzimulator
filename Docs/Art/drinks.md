# Drink packaging

Approved drink lineup: ReturAqua 500 ml, Loca Cola 330 ml, Hamster Energy 500 ml. Hamster uses the approved black/neon-green jagged H design; the user requested a half-litre can. Its model is taller and slightly wider than the 330 ml cola.

## Production assets

- Shared generated albedo: `Assets/Game/Art/Drinks/DrinkLabels.png` (1024 x 1536, three horizontal strips: cola, energy, water).
- Meshes and materials: `Assets/Game/Art/Drinks/`.
- Prefabs: `Assets/Game/Resources/Drinks/ReturAqua.prefab`, `LocaCola.prefab`, `HamsterEnergy.prefab`.
- Transparent faceted PET shading: `Assets/Game/Shaders/PET.shader`.
- Model preview: `Builds/DrinkPreview/drinks.png`; actual stocked shelf: `Builds/DrinkPreview/shop-drinks.png`.
- Regenerate and apply: **Nemzeti Dohánybolt > Apply Drink Packaging** in Edit mode. Existing counter and room transforms are retained; reapplying does not duplicate assets or docks.

Cans have shaped shoulders, metal rims, lid, bottom, drinking opening and a metal pull tab. ReturAqua has a faceted bottle, visible internal water level, transparent rim shading and a ribbed blue cap. The drink label uses one shared texture/material. Visual meshes have no colliders; existing product-root colliders handle picking and placement. `ProductVisuals.Size` supplies consistent dimensions for products, carton fitting, sample models and prefabs.

## Gameplay integration

The former placeholder water/gum/beer entries become water/cola/energy at the existing public catalog indices 4, 5, 6. Scene slot restrictions, labels, dock names and materials are migrated. Prices/costs: water 350/150 Ft, cola 450/180 Ft, energy 650/300 Ft. Ordering remains three units per paid shipment with an eight-second delivery. Drinks unlock with the self-service expansion; the starting shop retains the four cigarette brands. Carton samples, held products, shelf stock and customer baskets use the same prefabs.

The wider regression run exposed and fixed an initial-stock bug: inactive optional-shelf slots could consume the seeding loop while products failed placement and remained without a slot. Seeding now skips inactive slots and counts only successful placements; the packaging upgrader orders optional slots last. A regression asserts that all eight initial packs per brand occupy active slots.

## Verification

Final Play-mode checks: DrinkPackagingChecks.Run 54; CigarettePackagingChecks.Run 54; CompactShopChecks.BaseDay 107; ExpandedDay 134; ComputerWorkflowChecks.Run 55; WarehouseChecks.InventoryAndTrash 36. Total 440 passed, zero failed. Unity console: zero errors and warnings after normalized shader import. Covered ordering locks, three separate deliveries, one-at-a-time shelf restocking, hand-held visuals, correct-volume dimensions, returns and stock conservation, customer self-service, checkout, computer controls and initial stock. Scene migration was repeated to check idempotency. Models and stocked shelves rendered using Unity.

This change updates the local Unity project. No new standalone/WebGL build or Pages publication has been performed.

## Image generation

Built-in image_gen tool was used, with the approved `Docs/Art/approved-packaging-concept.png` as the style reference. The first generated atlas was revised to change only Hamster's volume from 250 ml to 500 ml. The final selected PNG is saved in the project; no runtime asset depends on the generation cache. Both exact prompts are recorded in `Docs/Art/drink-labels-prompt.txt`.
