# Prototype asset provenance

Created: 2026-09-21

| Asset | Author/source | Conditions |
|---|---|---|
| Runtime floor, walls, tanks, projectiles | Project-authored `PrimitiveFactory.cs` / `GameSession.cs`; Unity standard Cube, Cylinder, Sphere | Generated locally at runtime; no downloads, source images, prompts, textures or imported models |
| `Assets/_Project/Data/{Floor,Wall,Player,Enemy,Trim,Projectile}.mat` | Project-authored `SliceProjectBuilder.cs` | Six flat colors, URP Lit, smoothness 0.05; editable serialized materials |
| `Assets/_Project/Data/*.asset` | Project-authored ScriptableObjects | Stage layout, gameplay and prototype presentation settings |
| Existing `Assets/Settings/*`, URP shaders and default GUI font | Existing Unity Universal 3D template / installed Unity packages | Retained bootstrap content; Unity-provided components under their existing Unity/package terms, not separately relicensed by this repository |

No third-party or AI-generated media was added. No external asset license or AI generation prompt applies to the new project-authored code and numeric settings. These do not establish a new repository-wide redistribution license. Unity built-ins remain subject to Unity terms; installed package license texts are included with their packages in Library/PackageCache (generated, not committed).

Prototype models and flat materials are temporary. Wood grain, bevelled production meshes, VFX, audio and final art approval remain outside this slice.
