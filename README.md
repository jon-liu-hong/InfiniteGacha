# Infinite Gacha

Infinite Gacha is an open-source Unity platform for experimenting with loot boxes, it is based upon Infinite Loot Box by D.Kao.
Infinite Loot Box: https://github.com/kalikao/InfiniteLootBox

Demonstration: https://youtu.be/MpUcOohgA-0

## Parameters

- Many parameters can be adjusted in Assets/StreamingAssets/Settings.ini.
- To add new parameters, edit Assets/Scripts/Settings.cs.

## Items

- Item models must have a single submesh and a single material.
- To add a new loot:
    - Put your model in Assets/Resources/LootObjectsActive.
    - Attach the Loot3D, LootProp and LootData component to it. 
    - Write the loot's name in the property. If empty, the prefab's name will be used.
    - Position, rotate, and scale accordingly.
    - Put a 512x512 inventory icon with the same name as the prefab in the Assets/Resources/InventoryIcons folder and set the texture type to Sprite in Import Settings.
- LootObjectsInactive is a convenience folder to put unused loot prefabs into.
- To make a screenshot you can use "Thumbnail.scene". Read "ScreenshotCamera.cs" for further instructions.

## Unity Version

If you have issues running the project, please try with Unity version 2018.3.4.
