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
  - Enter text prompts to generate **3D Gaussian Splat** assets
  - Display **3D Gaussian Splat** assets inside Unity
  - Perform basic transformations on **3D Gaussian Splats**

## Installation

### Software requirements
Unity 2022.3+

### Instructions

#### 1. Open Unity
- From Unity Hub, create a new 3D project (any pipeline)

#### 2. Add the package
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

For projects using the **Built-In Render Pipeline**, installation is complete. For others, please see the [Universal Render Pipeline (URP)](#universal-render-pipeline) and [High Definition Render Pipeline (HDRP)](#high-definition-render-pipeline) sections below.

### Universal Render Pipeline
Unity offers three main rendering pipelines:
 - Built-In Render Pipeline (Standard) 
 - Universal Render Pipeline (URP) 
 - High Definition Render Pipeline (HDRP)

The main difference lies in their target applications: the Built-In pipeline is the legacy and general-purpose option, URP balances quality and performance across platforms, and HDRP maximizes visual quality for high-end systems.

Unity's default URP project template has three quality levels: Balanced, High Fidelity, and Performant. These can be seen in **Edit > Project Settings** under the **Quality** section.

  <img alt="Quality Render Pipeline Assets" src="./Documentation~/Images/Quality Render Pipeline Assets.png">

As rendering Gaussian Splats differs from rendering 3D models represented with meshes and textures, a **Renderer Feature** must be added to the **URP Renderer Asset** for each quality level.

1. From the **Project** folder, go to **Assets > Settings**
2. Select the asset labeled `URP-Balanced-Renderer`
   
  <img alt="URP Assets and Universal Renderer Data" src="./Documentation~/Images/URP Assets and Universal Renderer Data.png">

3. In the **Inspector**, under **Renderer Features**, click **Add Renderer Feature**
4. Select **Gaussian Splat URP Feature**

  <img alt="Add Renderer Feture" src="./Documentation~/Images/Add renderer Feature.png">
  
5. Repeat this process for the assets labeled `URP-HighFidelity-Renderer` and `URP-Performant-Renderer`

> [!NOTE]
> Unity 6 projects require enabling [Compatibility Mode (Render Graph Disabled)](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/compatibility-mode.html) in URP graphics settings to use custom  implementation of Scriptable Render Pass without using the render graph API.
> 
> <img alt="Add renderer feature" src="./Documentation~/Images/Compatibility Mode in Project settings marked.png">
> 
> The setting is in Project Settings > Graphics > Pipeline Specific Settings > URP > Render Graph.

### High Definition Render Pipeline 

HDRP uses volumes, allowing the scene to be partitioned into areas with their own specific lighting and effects. A Custom Pass Volume can be injected into the render loop, either globally or within a certain area.

1. Go to **Game Object > Create Empty** to add a new Game Object to the scene and name it `GaussianSplatEffect`.
2. In the **Inspector**, use the **Add Component** button to add a **Custom Pass Volume** either by searching for its name or by following the path: `Scripts > UnityEngine.Rendering.HighDefinition > Custom Pass Volume`

  <img alt="Custom Pass component" src="./Documentation~/Images/Custom Pass component.png">

3. Set the **Mode** to **Global** unless you only intend to use Gaussian Splats in a certain area of the scene, defined by colliders.

  <img alt="Custom Pass Mode" src="./Documentation~/Images/Custom Pass Mode.png">

4. Set the **Injection Point** to either **Before Transparencies** or **After Post Process** (recommended).

  <img alt="Custom Pass Injection point" src="./Documentation~/Images/Custom Pass Injection point.png">


## Usage
### Generating
1. Go to **Window > 404-GEN 3D Generator** to open the generation window
2. Type your prompt and click Generate. Each generation should take **1 to 2 minutes**

<img alt="Enable unsafe code" src="./Documentation~/Images/Prompts.png">

The 404-GEN 3D Generator window tracks the progress of generating the models for prompts.
Once the prompt has been enqueued, it waits on the backend to complete the generation.

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


For questions or help troubleshooting, visit the Help Forum in our [Discord Server](https://discord.gg/404gen)
