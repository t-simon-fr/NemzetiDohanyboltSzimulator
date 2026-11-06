# Albro és La Vazze alapanyagcsomagok

Jóváhagyott [látványreferencia](approved-reference.png), implementáció: #115. Az Albro kupakos, növényi italos karton, a La Vazze lezárt kávétéglatest. Mindkét termék továbbra is egy 10 adagos kávégépes alapanyag-szállítmány: a csomagolás 1 L / 250 g jelölése nem módosítja a meglévő recepteket vagy árakat.

Az `imagegen` skill beépített képgenerátorával a referencia két frontgrafikája külön, sík albedo-textúraként készült:

- `Assets/Game/Art/Ingredients/AlbroFront.png`: törtfehér alap, kék „albro” és „ZABITAL”, zöld levelek/hullám, zabillusztráció, „1 L”.
- `Assets/Game/Art/Ingredients/LaVazzeFront.png`: sötétkék alap, krém „La Vazze”, „ŐRÖLT KÁVÉ”, barna sáv és csésze, „250 g”.

Generálási promptok: „Use the approved product concept as reference. Reconstruct only the selected package's front printed artwork as a full-bleed flat 2D albedo texture; preserve the brand spelling, colours, illustration and composition. No package geometry, cap, bevels, folds, perspective, studio background, lighting or cast shadows.” Albro: álló 1:2 arány, a fenti tejgrafika; La Vazze: álló 3:4 arány, a fenti kávégrafika. A képgenerálás képi referenciából történt, a modelleket az Editor builder készíti.

Újragenerálás: `IngredientPackagingSetup.CreateAssets()` csak az Ingredients art/prefab asseteket írja, jelenetet nem ment. Prefabok: `Assets/Game/Resources/Ingredients/Albro.prefab`, `LaVazze.prefab`. A vizuális prefabok collider nélküliek; az alapanyag gyökere egy fizikai BoxCollidert kap. A textúrák és anyagok importált, megosztott assetek. A kávé nyomtatott felületei a hajtásokat követik és a test megfelelő felületeit helyettesítik, így nincs fedés.

A márkanevek játékbeli fantázianevek; ez a megvalósítás nem jelent márkajogi megfelelőségi állásfoglalást.
