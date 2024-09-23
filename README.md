# Unity package for 404-GEN 3D Generator
[![Discord](https://img.shields.io/discord/1065924238550237194?logo=discord&logoColor=%23FFFFFF&logoSize=auto&label=Discord&labelColor=%235865F2)](https://discord.gg/404gen)

*404—GEN leverages decentralized AI to transform your words into detailed 3D models, bringing your ideas to life in just a few seconds*  
[Project Repo](https://github.com/404-Repo/three-gen-subnet) | [Website](https://404.xyz/) | [X](https://x.com/404gen_)

## About
- This repository is specifically for the Unity package and does not include the 404—GEN Discord bot, Blender add-on or web front-end.  
- With this package, users can:
  - Enter text prompts to generate **3D Gaussian Splats** assets
  - Display **3D Gaussian Splats** assets inside Unity
  - Edit **3D Gaussian Splats**

## Installation

### Software requirements
Unity 2022.3+

### Instructions
To install the package:

* Open Unity.
* Go to **Window > Package Manager**
* Click the + button in the top-left corner
* Select Add package from git URL...
* Enter this GitHub repository's URL
* Go to **Edit > Project Settings...** and go to the **Player** section
* Make sure that the correct rendering backend is selected
    - D3D12 on Windows
    - Metal on Mac
    - Vulkan on Linux
* Make sure that **Allow unsafe code** is checked
<img width="480" alt="install" src="https://github.com/404-Repo/404-gen-unity-plugin.git/Images/project_settings.png">

## Usage
### Generating
1. Go to **Window > 404-GEN 3D Generator** to open the generation window.
2. Type your prompt and click Generate. Each generation should take **20 to 30 seconds**.
