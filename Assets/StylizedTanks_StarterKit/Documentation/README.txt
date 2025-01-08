1. FBX Models
2. Shaders
	2.1 Tank_Shader
	2.2 Track_Shader
3. Textures
	3.1 tank_gradients.png
4. Example
	4.1 Tank_example_Scene
	4.2 TankController Script

1. FBX Models
- There are two variations of the models, one with separated objects parented in a hierarchy and one with two skinned mesh and bones.
- The models use a special gradient texture where the vertical coordinate is the brightness/sample of the color gradient, and the horizontal is the variant.
- The models contain a vertex color channel as a mask for the non-painted parts like rubber or wood to exclude from offsetting.
- The tracks use two UV channels, one for the gradient color and the other for the track link tileable texture.
- The separated models contain two bones used by the skinned mesh of the track. The hull and wheel models are parented to them so jiggle/lean animation can be achieved.
- The skinned models contain an armature/skeleton and a single mesh for the hull, and another for the tracks skinned to the bones. 
- In addition, empty nodes are included with the "fx_" prefix for optional positioning of different vfx animations or particles. For example "fx_exhaust" and "fx_gun_muzzle".

2. Shaders
Two shader graphs are provided as examples for the intended use of the models.
2.1 Tank_Shader.shadergraph
ColorOffset property
The vertical gradient texture contains 12 different paint variations for the tank.
The vertex color channel can be used as a mask for the non-painted parts to be excluded from the offset.

2.2 Track_Shader.shadergraph
TrackOffset property
This value can be used to offset the track textures. To ensure the tracks are in sync with the tank movement and avoid losing precision, check the provided TankController example script.

3. Textures
3.1 tank_gradients.png
Universal texture for all of the assets in the pack. The TankController script uses these values to set the different paint colors.
1: team color green
2: team color blue
3: team color red
4: team color orange
5: German dark grey
6: German desert
7: Russian green
8: Russian white (or generic winter)
9: UK green
10: UK desert
11: US green
12: simple grayscale for optional resample of the color with a gradient.

4. Example
Two example Scenes are provided to showcase the setup and possible use of the content of the FBX files.
4.1 Tank_example_Scene
WASD: move the tank around
Mouse: Point the turret to the mouse position
1-9: Change the tank color 

4.2 TankController.cs Script
The example scene uses this script to control the tank and present the capabilities and intended use of the models.
There is another version of this file for the skinned vehicles but mostly differs only in how the materials are gathered from the model.

View Camera: Need to set the scene camera for the raycasting used by the turret rotation.
Hull Bone Transform: Pick the Bone_hull object from the model to manipulate the hull leaning.
Turret Transform: Pick the turret object to allow the rotation of the turret. 
Track Transform: Pick the track object to allow the track to offset while moving the vehicle.
Team Color: Set the offset of the painting. The vertex color is used to mask out non-painted parts like wood and rubber.
Speed: The maximum speed of the tank.
Turn Speed: The maximum turning speed.
Turret Speed: The speed of the turret rotation.

Track Multiplier: To ensure the uniformity of the track textures, narrower tracks have to use a different speed of offset.
1.0 for Tank_R_BT7, Tank_UK_Crusader
0.75 for the rest.

Hlean Speed: Speed of the hull leaning sideways when turning.
Vlean Speed: Speed of the hull leaning forward and backward when accelerating or decelerating.
H Max Lean: The maximum angle of leaning sideways.
V Max Lean: The maximum angle of leaning forward and backward.
