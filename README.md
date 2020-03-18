## Unity Lightmap Prefab Baker

The idea of prefab baker is to allow adding prefabs with baked lightmaps during play into the active scene  
  
If you want to buy me a beer  
[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.me/wad1m)
  
## Screenshots

| Interface | Example Bake Dark Scene Settings | Lightmap files after bake |
|------------|-------------|-------------|
| <img src="https://raw.githubusercontent.com/nukadelic/Unity-Lightmap-Prefab-Baker/master/Images~/image1.png" width="250"> | <img src="https://raw.githubusercontent.com/nukadelic/Unity-Lightmap-Prefab-Baker/master/Images~/image2.png" width="250"> | <img src="https://raw.githubusercontent.com/nukadelic/Unity-Lightmap-Prefab-Baker/master/Images~/image3.png" width="250"> | 

## Usage

1. Add "Prefab Baker" Script to any Game Object or Prefab 
2. Open baker window ( Window -> Prefab Baker ) 
3. Bake !

## How is it done ? 

Unity will bake lightmaps for the current active scene, the resulted image files will be moved to the target prefab folder. The prefab baker script in the prefab object will store those lightmaps references relative to each rendered component inside the prefab alongside the index, UV's scale offset. Once the prefab is added to the scene it will scan the current existing lightmaps and add any missing lightmaps.

## Notes:

- Make sure the lighting settings of your bake scene will match your level scene ( same skybox light source and directional lights if any )

- Quick bake was made for rapid lightmap prototyping which will use low quality for quick bake preview ( using this potions will not override the current scene lighting settings ) 

- When baking multiple prefabs their lights will effect each others lightmaps if they are spaced closely together, its recommended to bake one prefab at a time to avoid this result or place the prefabs far away from each other. 

- Adding the script to an object which is not a prefab and clicking bake, will automatically convert the game object into a prefab and save it in the same folder as the current open scene

- By default the lightmaps export folder will be located in Resouces/Lightmaps.

- The lightmaps will be copied to a new folder inside the selected target folder. To avoid lightmap collisions, make sure all baked prefabs have unique names across the project. 

## Installation

Simply download the **unitypackage** file from [here](https://github.com/nukadelic/Unity-Lightmap-Prefab-Baker/releases)  

**Or** add the following line to **manifest.json** file in your project packages folder (`UnityProject/Packages/manifest.json`)
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

**Or** pull the project locally and use the Package Manager (Window>Package Manager), adding the package.json file present in the root of the folder with the `+` button.

## Credit 

This tools is based on few similar implementations that can be found on this thread:   
https://forum.unity.com/threads/problems-with-instantiating-baked-prefabs.324514

  
  
footnote: feel free to open an issue if anything seems to be out of place.
