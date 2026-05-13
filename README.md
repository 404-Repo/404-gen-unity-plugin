# Unity Package for 404-GEN 3D Generator
[![Discord](https://img.shields.io/discord/1065924238550237194?logo=discord&logoColor=%23FFFFFF&logoSize=auto&label=Discord&labelColor=%235865F2)](https://discord.gg/404gen)

*404-GEN transforms prompts and reference images into 3D assets directly inside Unity.*

[Project Repo](https://github.com/404-Repo/three-gen-subnet) | [Website](https://404.xyz/) | [X](https://x.com/404gen_)

## Overview
404-GEN gives Unity creators a built-in window for generating and working with 3D assets:

- Generate **3D Gaussian Splats (3DGS)** from text or image prompts.
- Generate **Mesh v2 GLB models** from image prompts.
- Display, edit, and render Gaussian Splat assets in Unity.
- Add cutouts, convex mesh colliders, and simple shadows to Gaussian Splats.
- Import and export Gaussian Splat `.ply` files.

3D Gaussian Splatting renders high-fidelity objects using many translucent ellipsoids, or "splats." Each splat stores color, size, position, and opacity data.

## Requirements
| Requirement | Details |
| --- | --- |
| Unity | Unity 2022.3 or newer |
| Network access | Required for 404-GEN cloud generation |
| API key | A generic limited API key is included. Users can get their own key at [gen.404.xyz](https://gen.404.xyz/) |
| Graphics backend | DirectX 12 on Windows, Metal on macOS, Vulkan on Linux |
| Package dependencies | Installed by Unity: Burst, Collections, Mathematics, Newtonsoft Json, Editor Coroutines, and glTFast |

## Installation
1. From the [Unity Asset Store](https://assetstore.unity.com/packages/tools/generative-ai/404-gen-3d-generator-311107), click **Add to My Assets**.
2. In Unity, open **Window > Package Manager**.
3. Select **My Assets**.
4. Select **404-GEN 3D Generator**.
5. Click **Download**, then **Import**.
6. Keep all files selected in the import window and click **Import**.
7. Restart Unity before using the plugin.

If you are updating from an older release, remove the previous version before importing the latest package.

## Quick Start
1. Open **Window > 404-GEN 3D Generator**.
2. Choose the output type:
   - **3DGS**: use a text prompt or image prompt.
   - **Mesh**: use an image prompt. Text-only mesh generation is not supported by Mesh v2.
3. For Mesh output, adjust **Geometry Quality**, **Texture Quality**, and **Face Count** if needed.
4. Click **Generate**.
5. Generated assets are saved to `Assets/GeneratedModels` by default.

The generation window tracks each job while it is queued, running, and completed.

## Output Types
| Output | Input | Result |
| --- | --- | --- |
| 3DGS | Text prompt or image prompt | Gaussian Splat asset created from the generated `.ply` data |
| Mesh | Image prompt only | `.glb` model imported into Unity through glTFast |

For prompt guidance, see the [404-GEN Prompt Guide](https://guide.404.xyz/user-guide/prompts).

## Settings
Open **Edit > Project Settings > 404-GEN 3D Generator** to configure:

- Generated models folder.
- Gateway URL and API key for 3DGS generation.
- Mesh v2 API URL and API key.
- Auto-cancel timeout for long-running prompts.
- Whether deleting a prompt also deletes associated generated files.

The default Mesh v2 settings generate detailed geometry and textures. Lower quality settings can reduce processing time and output size.

## Gaussian Splatting Tools
### Transformations
Gaussian Splat renderers expose two inspector controls in addition to standard Transform values:

- **Splat Scale** controls the size of the rendered splats.
- **Opacity Scale** increases or decreases the opacity of all splats.

### Cutouts
Use **Add Cutout** in the Gaussian Splat inspector to hide or isolate parts of a splat with a box or ellipsoid cutout. Enable **Invert** on the cutout to render the area outside the shape instead.

### Mesh Collider
Use **Add Mesh Collider** in the Gaussian Splat inspector to add a lightweight convex hull collider generated from the splat positions.

### Shadows
Use **Add Shadow** in the Gaussian Splat inspector to add an invisible convex hull mesh that casts a simple shadow for the splat object.

### Import and Export PLY
Use **Export PLY** in the Gaussian Splat inspector to export the selected splat as a `.ply` file.

`.ply` files can be imported by adding them to the project's `Assets` folder and placing the imported asset in the scene.

A collection of Gaussian Splatting `.ply` files is available in the [404 Dataset](https://dataset.404.xyz).

## Troubleshooting
| Symptom | What to try |
| --- | --- |
| Mesh generation button is disabled | Mesh v2 requires an image prompt. Select an image before generating Mesh output. |
| Generated GLB does not import as a model | Confirm the package dependency `com.unity.cloud.gltfast` is installed by Unity. |
| Job times out | Increase the timeout in **Edit > Project Settings > 404-GEN 3D Generator**. |
| Job reports unauthorized | Check the API key in Project Settings, or get a user API key at [gen.404.xyz](https://gen.404.xyz/). |
| Rendering issues | Confirm the recommended graphics backend is enabled for your platform. |

For questions or help troubleshooting, visit the Help Forum in the [404-GEN Discord Server](https://discord.gg/404gen).

## License
This package is distributed under the [MIT License](./LICENSE.md).
