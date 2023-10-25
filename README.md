# unit-information
Provided as-is from the last version of the prototype.
A proof-of-concept hide-and-seek game; information gets outdated as each unit only knows what was seen by them; and what was told to them.


The implementation of this project was pretty straightforward, shortcuts were taken whenever possible to see if this idea goes anywhere.


- Rigidbodies are tracked on the grid/memory as '[MovingVirtuals](https://github.com/ucanluc/unit-information/blob/a702a6e1d447f4c359e57c554344634213fc7f20/Assets/Scripts/GameScene/MovingVirtual.cs#L42)' and handled further via uids.
- Field of view (shadowcasting) and A* implementation are mostly generic and works on a 2D grid.
- Pathfinding is calculated on the physical map in this version to speed the game up.
- Latest known positions of each unit are displayed as [spheres](https://github.com/ucanluc/unit-information/blob/a702a6e1d447f4c359e57c554344634213fc7f20/Assets/Scripts/GameScene/UnitMarker.cs#L7) 
- AI and scene-view changes are done from [UnitKnowledge](https://github.com/ucanluc/unit-information/blob/a702a6e1d447f4c359e57c554344634213fc7f20/Assets/Scripts/Knowledge/UnitKnowledge.cs#L91); which seemed to be the fastest way of getting it working.
- The main exploration was using unit memory as the backbone for a game, and keeping an individual map state on every unit.


Delays in communication and outdated information are not common mechanics in games (as far as I know), and exploring this idea further seems promising.
A rework in Godot to try out additional features is in the works.


- [Animations are from this repository](https://github.com/Unity-Technologies/Standard-Assets-Characters)
- [Characters are from this free asset](https://assetstore.unity.com/packages/3d/props/polygon-starter-pack-low-poly-3d-art-by-synty-156819)
- [Skyboxes are from this free asset](https://assetstore.unity.com/packages/2d/textures-materials/sky/skybox-series-free-103633)
- Last opened in Unity version 2022.3.10f1
