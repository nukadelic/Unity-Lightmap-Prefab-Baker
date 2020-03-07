## Unity Lightmap Prefab Baker

## Screenshots

| Interface | Example Bake Dark Scene | Lightmap files after bake |
|------------|-------------|-------------|
| <img src="https://raw.githubusercontent.com/nukadelic/Unity-Lightmap-Prefab-Baker/master/Images~/image1.png" width="250"> | <img src="https://raw.githubusercontent.com/nukadelic/Unity-Lightmap-Prefab-Baker/master/Images~/image2.png" width="250"> | <img src="https://raw.githubusercontent.com/nukadelic/Unity-Lightmap-Prefab-Baker/master/Images~/image3.png" width="250"> | 

## Installation

Download the **unitypackage** file from [here](https://github.com/nukadelic/Unity-Lightmap-Prefab-Baker)

Add the following line to **manifest.json** file in your project packages folder ( `UnityProject/Packages/manifest.json` )
```
"com.nukadelic.prefabbaker": "https://github.com/nukadelic/Unity-Lightmap-Prefab-Baker.git"
```
so it would look something like that : 
```
{
  "dependencies": {
    "com.nukadelic.prefabbaker": "https://github.com/nukadelic/Unity-Lightmap-Prefab-Baker.git",
    "com.unity.collab-proxy": "1.2.16",
    "com.unity.ext.nunit": "1.0.0",
    "com.unity.ide.rider": "1.1.0",
    ...
  }
}
```

Or pull the project locally and use the Package Manager (Window>Package Manager), adding the package.json file present in the root of the folder with the `+` button.
