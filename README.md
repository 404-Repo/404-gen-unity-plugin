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
  - Enter text prompts to generate **3D Gaussian Splats** assets
  - Display **3D Gaussian Splats** assets inside Unity

## Installation

### Software requirements
Unity 2022.3+

### Instructions

#### 1. Open Unity
- From Unity Hub, create a new 3D project (Built-In Render Pipeline)

#### 2. Add the package
* Go to **Window > Package Manager**
* Click the **+** button in the top-left corner
* Select Add package from git URL...
* Enter this GitHub repository's URL: `https://github.com/404-Repo/404-gen-unity-plugin.git`
  
  <img width="480" alt="add-package" src="https://github.com/user-attachments/assets/5cacacb4-2893-4923-a333-4c1c0ca854fc">

#### 3. Edit Project Settings
* Go to **Edit > Project Settings...** and go to the **Player** section
* Make sure that the correct rendering backend is selected
    - D3D12 on Windows
    - Metal on Mac
    - Vulkan on Linux
* Make sure that **Allow 'unsafe' code** is checked

  <img width="480" alt="install" src="https://github.com/404-Repo/404-gen-unity-plugin/blob/main/Images/project_settings.png?raw=true">

## Usage
### Generating
1. Go to **Window > 404-GEN 3D Generator** to open the generation window
2. Type your prompt and click Generate. Each generation should take **20 to 30 seconds**

### Prompts
A prompt is a short text phrase that 404—GEN interprets to create a 3D Gaussian Splat. In this section, we’ll explore how to craft clear and effective prompts, along with tips to refine your input for the best results.
Describe a single element or object, rather than an entire scene. A good rule of thumb is something you can look in toward, rather than out toward, regardless of size. "Sky" wouldn't work, but "planet" would.
Try to be specific but not overly verbose. Prompts between 2 and 12 words in length tend to produce the best results.
You can experiment with keywords for specific styles or materials. A few examples are:
  - Anime
  - Chibi
  - Clay
  - Cute
  - Oil Pastel
  - Papercraft
  - Psychedelic
  - Voxel

> [!NOTE]
> If the network is busy, the operation will automatically be canceled after 1 minute. Try again

For questions or help troubleshooting, visit the Help Forum in our [Discord Server](https://discord.gg/404gen)
