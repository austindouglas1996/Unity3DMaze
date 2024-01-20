<h1>MazeGame</h1>
A simple 3D dungeon maze generation in Unity3D that leverages reusable room prefabs and interconnected doorways to create immersive and varied environments. 
This technique departs from traditional cell-based graph methods, but still uses some cell-based systems to help with pathfinding and creation of hallways.

![image](https://github.com/austindouglas1996/Unity3DMaze/assets/36781465/59292230-5198-4898-bc54-22e27c58a24e)


<h1>Features</h1>

- Generate 2D or 3D mazes using simple, easy to make blueprint prefabs.
- Construct elaborate interconnected paths with a hallways.
- Utilize an easy-to-use prop system to create dynamic beautiful environments.
- Implement a simple A* pathfinding system for enemy movement.


<h1>Planned</h1>

- Rendering system to deal with large mazes.
- Transition to fully 3D hallway connections for immersive environments. (Currently only X and Z are supported)
- Implement a better A* algorithm cell-based navigation. Currently pathfinding does not take distance in affect and some pathfindings will find some interesting ways to reach destinations.
- Room blueprint editor to create generic rooms faster, and more efficently.

<h1> Rooms. </h1>
<p>The generator supports generic prefabs to fully customized rooms, using Unity's prefab system that fosters flexibility and diversity. Doorways act as connection points, ensuring each maze iteration offers a distinct and different experience. </p>

![image](https://github.com/austindouglas1996/Unity3DMaze/assets/36781465/25e47c58-8d50-468f-aa27-e35eaa61b53e)

![image](https://github.com/austindouglas1996/Unity3DMaze/assets/36781465/3927b2e4-5af3-41a6-979a-12a165df9aac)

<h1>Hallways</h1>
<p>To ensure varied and unpredictable maze layouts, the system employs a hallway generator that operates using a generic room blueprint on repeat. 
  It identifies unconnected doorways within the maze and constructs hallways between them, fostering multiple pathways to reach destinations and creating a unique experience with each new maze generation. Built-on of the room generator helps with easy transitions
allowing the hallways to seamlessy fit in.</p>

![image](https://github.com/austindouglas1996/Unity3DMaze/assets/36781465/18e753a1-9786-4a6e-a4c7-54784e079f17)
![image](https://github.com/austindouglas1996/Unity3DMaze/assets/36781465/a9158c54-9424-4ee2-87f9-f9dcb38e0fb1)

<h1>Props</h1>
<p>Props are a vital part to making any maze neat and different. With a simple prop system allowing you to create simple environments. Throuhgout my examples I used an abandoned castle system. Throughout your blueprints fixtures are assigned simple prop size values
to help the generator control what type of props are allowed to spawn, then the system will automatically select props based on this. </p>

![image](https://github.com/austindouglas1996/Unity3DMaze/assets/36781465/8b8c8ce9-ccd8-4ff6-a733-c04124196b69)

<p>Fixtures prop options</p>

![image](https://github.com/austindouglas1996/Unity3DMaze/assets/36781465/3f399bfe-fb1a-47b8-bc9d-9dd0080be6a7)

<p>With props on:</p>

![image](https://github.com/austindouglas1996/Unity3DMaze/assets/36781465/7e86af12-d25a-4ea5-870c-d2e03bed5481)

<p>With props off:</p>

![image](https://github.com/austindouglas1996/Unity3DMaze/assets/36781465/52806ff6-0355-4f43-b1b8-f4a18f6b161b)

<h1> How to implement. </h1>
<p>Implementing this sytem at this time does take a few steps. This section will detail how to implement using the example pieces provided. If you'd like to create using a system from scratch please see the docuemntation.</p>

1. Create an empty object and assign the **MazeController** and **MazeResourceStore** (This object is a global). 
2. In the **MazeResourceStore** create a theme and assign some basic prefabs for walls, floors, roofs, doors, respective traps, and generic props.

![image](https://github.com/austindouglas1996/Unity3DMaze/assets/36781465/4b9682a2-15e7-4a3f-8301-c98259a6f6c5)

3. In the **MazeController** there is a **RoomGenerator** object, add any generic & special rooms that should appear in the maze. Included are 20 generic rooms.
   
![image](https://github.com/austindouglas1996/Unity3DMaze/assets/36781465/5ad5a951-a8f9-4400-b222-c4daf5bc5a53)

5. If you're going to include hallways, in the **HallwayGenerator** a hallway prefab needs to be set (This is also included)

<h1>Unity packs used in the example</h1>
<p>I am not a designer, so I used many Unity packs to help with my creations. In the pictures above I have used a lot of different props from "Syntey Studios" packs. They are easy to use and they look nice. Here is the list used to create the maze:</p>

- [Cartoon Grass (Props)](https://assetstore.unity.com/packages/3d/vegetation/cartoon-grass-and-plants-221724)
- [Low Poly Dungeons (Walls,Floors,Props)](https://assetstore.unity.com/packages/3d/environments/dungeons/low-poly-dungeons-176350)
- [Polygon Dungeon (Walls,Floors,Props)](https://assetstore.unity.com/packages/3d/environments/dungeons/polygon-dungeons-low-poly-3d-art-by-synty-102677)
- [Polygon Fanstasy Kingom](https://assetstore.unity.com/packages/3d/environments/fantasy/polygon-fantasy-kingdom-low-poly-3d-art-by-synty-164532)
- [Polygon Horror Mansion (Props)](https://assetstore.unity.com/packages/3d/environments/fantasy/polygon-horror-mansion-low-poly-3d-art-by-synty-213346)
- [Polygon Prototype (Materials)](https://assetstore.unity.com/packages/3d/props/exterior/polygon-prototype-low-poly-3d-art-by-synty-137126)



