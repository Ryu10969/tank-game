# Prototype asset provenance

Created: 2026-09-21

| Asset | Author/source | Conditions |
|---|---|---|
| Runtime floor, walls, tanks, projectiles, Mine explosion VFX | Project-authored `PrimitiveFactory.cs` / `GameSession.cs`; Unity standard Cube, Cylinder, Sphere | Generated locally at runtime; Mine VFX reuses the project-authored Mine material and Sphere primitive; no downloads, source images, prompts, textures or imported models |
| `Assets/_Project/Data/{Floor,Wall,DestructibleWall,Mine,Player,Enemy,Heavy,Burst,Trim,Projectile}.mat` | Project-authored `SliceProjectBuilder.cs`; generated locally in this repository | Ten flat colors, URP Lit, smoothness 0.05; Heavy green and Burst blue are graybox identification colors; no external source, download, texture, prompt, or separate third-party license |
| `Assets/_Project/Data/*.asset` | Project-authored ScriptableObjects | Stage layout, gameplay and prototype presentation settings |
| Existing `Assets/Settings/*`, URP shaders and default GUI font | Existing Unity Universal 3D template / installed Unity packages | Retained bootstrap content; Unity-provided components under their existing Unity/package terms, not separately relicensed by this repository |

No third-party or AI-generated media was added. No external asset license or AI generation prompt applies to the new project-authored code and numeric settings. These do not establish a new repository-wide redistribution license. Unity built-ins remain subject to Unity terms; installed package license texts are included with their packages in Library/PackageCache (generated, not committed).

Prototype models and flat materials are temporary. Wood grain, bevelled production meshes, polished VFX, audio and final art approval remain outside this slice.
