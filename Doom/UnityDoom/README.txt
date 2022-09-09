=== Unity Doom ===

Unity Doom is a port of Managed Doom (https://github.com/sinshu/managed-doom) to Unity.

To place an interactive playable Doom screen in your Unity game, add the UnityDoom.UnityDoomPlayer component to an object and check `play`. For more in-depth usage, read the source code.

The plugin will read DOOM WADs and the included sound font file from the specified static file directory (if blank, defaults to your StreamingAssets folder). If the files are not present in this folder, Doom will fail to run.


== LICENSE ==
Unity Doom and Managed Doom are distributed under the GPLv2 license.

Managed Doom uses the following libraries:
- TimGM6mb by Tim Brechbill (GPLv2 license)
- MeltySynth (MIT license)