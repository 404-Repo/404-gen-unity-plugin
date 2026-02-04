# Unity Package for 404—GEN 3D Generator
[![Discord](https://img.shields.io/discord/1065924238550237194?logo=discord&logoColor=%23FFFFFF&logoSize=auto&label=Discord&labelColor=%235865F2)](https://discord.gg/404gen)

*404—GEN leverages decentralized AI to transform your words into detailed 3D models, bringing your ideas to life in just a few seconds*  
[Project Repo](https://github.com/404-Repo/three-gen-subnet) | [Website](https://404.xyz/) | [X](https://x.com/404gen_)

## About
### 3D Gaussian Splatting

3D Gaussian Splatting is a technique used for efficient representation and rendering of three-dimensional objects by leveraging Gaussian distributions.
This technique renders high fidelity objects using lots of tiny translucent ellipsoids or "splats." Each splat carries information about its color, size, position, and opacity.

### Unity Package
  
- With this package, users can:
  - Enter Text or Image Prompts to generate **3D Gaussian Splats (3DGS)** and **Mesh FBX Models**.
  - Display **3D Gaussian Splat** assets inside Unity.
  - Apply **cutouts**, **colliders** and **shadows** to **3D Gaussian Splats**.
  - Import and Export **.ply** files.
  - Convert **3DGS** to **Mesh**.

## Installation

### Software requirements
Unity 2022.3+

### Instructions
*Previous Release must be Removed before Installing Latest Release.*

### 1. Unity Asset Store
- From the [Unity Asset Store](https://assetstore.unity.com/packages/tools/generative-ai/404-gen-3d-generator-311107), click "Add to My Assets".

### 2. Download the Package.
- In Unity, create a new 3D Project or open and existing one.
- Go to **My Assets**.
- Select 404—GEN from the list.
- Click **Download**.

### 3. Install The Package.
- After the package has been downloaded click **Import**.
When the import Window Appears, keep all files selected and click **Import**.

### 4. Restart Unity.
- **Please restart Unity before using the Plugin.**

Make sure the rendering backend is now set to 
- Directx 12 on Windows.
- Metal on Mac OS.
- Vulkan on Linux.


## Usage
### Generating
1. Go to **Window > 404-GEN 3D Generator** to open the generation window.
2. Type your **Text Prompt** or Import your **3D Image Prompt** and click **Generate**. Each generation should take **1 to 2 minutes**.

<img alt="Enable unsafe code" src="./Documentation~/Images/Prompts.png">

The 404-GEN 3D Generator window tracks the progress of generating the models for prompts.
Once the prompt has been enqueued, it waits on the backend to complete the generation.


    
### Prompts
For help with prompts please refer to our [Prompt Guide](https://guide.404.xyz/user-guide/prompts)

For questions or help troubleshooting, visit the Help Forum in our [Discord Server](https://discord.gg/404gen)

### Gaussian Splatting Tools
#### Transformations
In addition to the Position, Rotation, and Scale values in the Inspector, there are two easily adjustable values.
* Splat Scale: Controls the size of the points, represented by ellipsoids, in the Gaussian Splat
* Opacity Scale: Points within Gaussian Splats have varying degrees of opacity. This increases or decreases the opacity of all points.

![gs-scaling](./Documentation~/Images/scaling.gif)

#### Cutouts
The cutouts feature can be used to hide a selection of points within the Gaussian Splat, defined by either a box or ellipsoid.

Create a cutout for a selected Gaussian Splat by clicking Add Cutout under the Cutouts heading of the Inspector.

<img width="480" alt="Screenshot 2025-05-29 at 9 01 26 AM" src="./Documentation~/Images/edit-resources.png" />

Select a shape and move/scale/rotate as needed.

By default, only the points **inside** the cutout will be rendered. Select **invert** to render the points outside the cutout.

<img width="680" alt="cutout" src="./Documentation~/Images/cutout.png" />

#### Mesh Collider
The **Add Mesh Collider** button in the Inspector will add a convex hull mesh collider.

![collider](./Documentation~/Images/collider.png)

#### Shadows
Like the mesh collider, the **Add Shadow** button in the inspector adds an invisible convex hull mesh to the Gaussian Splat. The shadow it casts lacks any fine details, however the convex hull's low poly count makes it very lightweight.

![shadow](./Documentation~/Images/shadow.png)

#### Convert to Mesh
The **Convert to Mesh** button will open a Mesh Conversion window with input field (for Gaussian Splat component), file output folder location, conversion settings, reference fields for files that are created in the process (.ply and .mesh files), as well as the reference field for the game object that will be created and placed in current scene (Instance).

<img width="500" alt="mesh-conversion" src="./Documentation~/Images/mesh-conversion.png" />

The sliders adjust the level of detail on the mesh output.

Once the **Start conversion to Mesh** button is used the process will start populating the object fields as they are created. The process takes approximately **one minute** to convert the Gaussian Splat (PLY) to mesh (FBX).

#### Import/Export PLY
The Gaussian Splat can be exported in .ply format using the **Export PLY** button.

.ply files can be imported by adding them to the project's Assets folder and the scene.

A large collection of Gaussian Splatting .ply files is available in the [404 Dataset](https://dataset.404.xyz).

<img width="800" alt="dataset" src="./Documentation~/Images/dataset.png" />

For questions or help troubleshooting, visit the Help Forum in our [Discord Server](https://discord.gg/404gen)
