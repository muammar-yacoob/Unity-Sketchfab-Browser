# Unity Sketchfab Browser

## Overview

The Unity Sketchfab Browser is a Unity Editor extension that allows developers to search, preview, and import 3D models from Sketchfab directly into their Unity projects. Leveraging the power of UniTask for asynchronous operations and Unity.XR for optimal rendering, the browser aims to streamline the workflow of 3D asset management in Unity.

## Features

- **Search Functionality**: Quickly find models based on keywords, categories, and more.
- **Preview**: View detailed previews of models before importing.
- **Asynchronous Tasks**: Uses UniTask for non-blocking operations.
- **UPM Support**: Easy installation through Unity's Package Manager.
- **Testable Code**: Follows SOLID principles and MVC architecture for easy testing.

## Installation ##
In Unity Package Manager (UPM) Add Package from git URL:<BR>
https://github.com/muammar-yacoob/unity-sketchfab-browser.git<br><br>
Or get the [Unity Package Installer](../../releases/download/v1.0.1/Install-sketchfabbrowser-latest.unitypackage)<br>

After installation, you will see the Sketchfab browser option under `[Assets > Sketchfab Browser]` in the Unity editor top menu bar.

### Model Browser
[![Model Inspector](./res/model-browser.png)](https://sketchfab.com/3d-models/starbutts-564e02a97528499388ca00d3c6bdb044)


## Usage
1. Open the Sketchfab Browser from `Assets > Sketchfab Browser`.
2. Enter your Sketchfab API token when prompted.
3. Use the search bar to find models by keyword.
4. Browse through models and click on them to see more details.
5. Click `Download` or `Buy` to add the model to your Unity project.

## Support and Contribution
Found this useful? A quick ⭐️ is much appreciated and could help you and others find the repo easier.
For issues, feature requests, or contributions, please open an issue or pull request on GitHub.