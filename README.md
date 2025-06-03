# Unity package for 404—GEN 3D Generator
[![Discord](https://img.shields.io/discord/1065924238550237194?logo=discord&logoColor=%23FFFFFF&logoSize=auto&label=Discord&labelColor=%235865F2)](https://discord.gg/404gen)

*404—GEN leverages decentralized AI to transform your words into detailed 3D models, bringing your ideas to life in just a few seconds*  
[Project Repo](https://github.com/404-Repo/three-gen-subnet) | [Website](https://404.xyz/) | [X](https://x.com/404gen_)

## About
#### 3D Gaussian Splatting

3D Gaussian Splatting is a technique used for efficient representation and rendering of three-dimensional objects by leveraging Gaussian distributions.
This technique renders high fidelity objects using lots of tiny translucent ellipsoids or "splats." Each splat carries information about its color, size, position, and opacity.

#### Unity package
  
- With this package, users can:
  - Enter text prompts to generate **3D Gaussian Splats** and **Mesh** FBX models
  - Display **3D Gaussian Splat** assets inside Unity
  - Perform basic transformations on **3D Gaussian Splats**
  - Convert **3DGS to mesh**

## Installation

### Software requirements
Unity 2022.3+

### Instructions

#### 1. Open Unity
- From Unity Hub, create a new 3D project (any pipeline)

#### 2. Add the package
* [Free Download from Unity Asset Store](https://assetstore.unity.com/packages/slug/311107)

or

* Go to **Window > Package Manager**
* Click the **+** button in the top-left corner
* Select **Add package from git URL...**
* Enter this GitHub repository's URL: `https://github.com/404-Repo/404-gen-unity-plugin.git`
  
  <img alt="Add package from Git" src="./Documentation~/Images/PackageManager.png">

#### 3. Edit Project Settings
* Go to **Edit > Project Settings...** and go to the **Player** section
* Make sure that the correct rendering backend is selected
    - **D3D12** on Windows
    - **Metal** on Mac
    - **Vulkan** on Linux
  
  <img alt="Set rendering backend" src="./Documentation~/Images/ProjectSettingsGraphicsAPI.gif">

* Check the **Allow 'unsafe' Code** box
  
  <img alt="Enable unsafe code" src="./Documentation~/Images/EnableUnsafeCode.gif">

## Usage
### Generating
1. Go to **Window > 404-GEN 3D Generator** to open the generation window
2. Select either Gaussian Splat or Mesh model

<img width="480" alt="gen-window" src="https://github.com/user-attachments/assets/ba4473bf-e350-4a82-b922-2b52f6d2809d"/>

  > [!IMPORTANT]
  > When Gaussian Splat is selected in **URP** and **HDRP** projects, a warning message will appear to load then add a custom renderer feature to the scene. This is required to render Gaussian Splatting.
3. Type your prompt and click Generate. Each generation should take **1 to 2 minutes**

<img alt="Enable unsafe code" src="./Documentation~/Images/Prompts.png">

The 404-GEN 3D Generator window tracks the progress of generating the models for prompts.
Once the prompt has been enqueued, it waits on the backend to complete the generation.
Mesh generation can take slightly longer, as the Gaussian Splat is first generated then converted to mesh.

Generation process changes states from <img alt="Started" src="./Editor/Images/pending.png" height="20"> Started to <img alt="Completed" src="./Editor/Images/complete.png" height="20"> Completed or 
<img alt="Failed" src="./Editor/Images/failed.png" height="20">  Failed.

Use available action icons to:

  * <img alt="Target" src="./Editor/Images/close.png" height="20">  cancel active prompt entry
  * <img alt="Target" src="./Editor/Images/hidden.png" height="20"> or <img alt="Target" src="./Editor/Images/visible.png" height="20"> show or hide created Gaussian splat model
  * <img alt="Target" src="./Editor/Images/target.png" height="20"> select generated model in Scene view and Inspector window
  * <img alt="Resend" src="./Editor/Images/retry.png" height="20"> resend failed or canceled prompt
  * <img alt="Log" src="./Editor/Images/logs.png" height="20">**LOGS** show log messages in a tooltip
  * <img alt="Delete" src="./Editor/Images/delete.png" height="20"> delete prompt entry
  * <img alt="Settings" src="./Editor/Images/settings.png" height="20"> open Project settings for this package

### Prompts
A prompt is a short text phrase that 404—GEN interprets to create a 3D Gaussian Splat. In this section, we’ll explore how to craft clear and effective prompts, along with tips to refine your input for the best results.
* Describe a single element or object, rather than an entire scene. A good rule of thumb is something you can look in toward, rather than out toward, regardless of size. "Sky" wouldn't work, but "planet" would.
* Try to be specific about colors, styles, and elements
* Be open-minded and flexible: you may need to re-phrase or add/remove parts of the prompt. Like any skill, prompting can take time to perfect

### Gaussian Splatting Tools
#### Transformations
In addition to the Position, Rotation, and Scale values in the Inspector, there are two easily adjustable values.
* Splat Scale: Controls the size of the points, represented by ellipsoids, in the Gaussian Splat
* Opacity Scale: Points within Gaussian Splats have varying degrees of opacity. This increases or decreases the opacity of all points.

![gs-scaling](https://github.com/user-attachments/assets/0828e35f-87bb-4438-81b2-af70d4730096)

#### Cutouts
The cutouts feature can be used to hide a selection of points within the Gaussian Splat, defined by either a box or ellipsoid.

Create a cutout for a selected Gaussian Splat by clicking Add Cutout under the Cutouts heading of the Inspector.

<img width="480" alt="Screenshot 2025-05-29 at 9 01 26 AM" src="https://github.com/user-attachments/assets/25c0a914-4e21-4aa8-b183-5fa236a06258" />

Select a shape and move/scale/rotate as needed.

By default, only the points **inside** the cutout will be rendered. Select **invert** to render the points outside the cutout.

<img width="680" alt="cutout" src="https://github.com/user-attachments/assets/e4ad9979-a5d4-4c6c-a0cc-f6f5d6170483" />

#### Mesh Collider
The **Add Mesh Collider** button in the Inspector will add a convex hull mesh collider.

![collider](https://github.com/user-attachments/assets/ec819c97-20f3-41b3-8232-a938da5194db)

#### Shadows
Like the mesh collider, the **Add Shadow** button in the inspector adds an invisible convex hull mesh to the Gaussian Splat. The shadow it casts lacks any fine details, however the convex hull's low poly count makes it very lightweight.

![shadow](https://github.com/user-attachments/assets/f2602708-ad43-4752-9aa8-57c03c1ab95f)

#### Convert to Mesh
The **Convert to Mesh** button will open a Mesh Conversion window with input field (for Gaussian Splat component), file output folder location, conversion settings, reference fields for files that are created in the process (.ply and .mesh files), as well as the reference field for the game object that will be created and placed in current scene (Instance).

<img width="500" alt="mesh-conversion" src="https://github.com/user-attachments/assets/7779fc58-038b-45e1-aa81-892c97209799" />

The sliders adjust the level of detail on the mesh output.

Once the **Start conversion to Mesh** button is used the process will start populating the object fields as they are created. The process takes approximately **one minute** to convert the Gaussian Splat (PLY) to mesh (FBX).

#### Import/Export PLY
The Gaussian Splat can be exported in .ply format using the **Export PLY** button.

.ply files can be imported by adding them to the project's Assets folder and the scene.

A large collection of Gaussian Splatting .ply files is available in the [404 Dataset](https://dataset.404.xyz).

<img width="800" alt="dataset" src="https://github.com/user-attachments/assets/23d772c1-725c-440f-96c8-3240dd58eae5" />

For questions or help troubleshooting, visit the Help Forum in our [Discord Server](https://discord.gg/404gen)
