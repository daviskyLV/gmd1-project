# Game dev blog post #1
The first milestone for the game was about generating the world and it sure was a hard task. Let's see how it went! **DISCLAIMER: A LOT TO READ, BECAUSE A LOT HAPPENED!**

## Attempt 1 - "flat" world made from a heightmap that wraps around
My main inspiration for the world were games like _Sid Meier's Civilization_ (Civ) and _Hearts of Iron 4_ (HoI4), both of which have maps that wrap around horizontally (along longitude). What I mainly wanted were continents like in Civ games and obviously on the Earth like in HoI4.
### Creating the heightmap
My main implementation followed closely **Sebastian Lague's** tutorial on [Procedural Landmass Generation](https://www.youtube.com/watch?v=MRNFcywkUSA) in particular on how to generate heightmap using _Perlin noise_ (although I used _Simplex noise_, as it produces less artifacts and is more efficient).
My main steps were quite similar to the tutorial:
1. Set variables (map size, seed, octaves, etc.)
2. Generate the heightmap for each octave and sum them up according to persistence
3. Calculate min and max height points
4. Normalize height to 0-1 values
### Divergence from the tutorial
In the tutorials Sebastian does most of the stuff on the **Unity's Main thread**, with the threading implementation later. I was also interested in [Unity DOTS](https://unity.com/dots) system, although converting everything to data oriented workflow seemed a bit too hard for me. Luckily, you can use DOTS only partially and still get performance gains.<br/>
So what I did? I converted the code to use **Unity Jobs** and **Burst** compilation!<br/>
Noise Calculation differences:
1. A slight annoyance was the use of `NativeArray` within Jobs, which meant I had to duplicate data when running a Job
2. Most obvious of all - flattening the 2D arrays used in the tutorial
3. Conversion to **Unity.Mathematics** methods, which are more optimized for Burst compilation, as well as trying to have less branches in code. In place of `if statements` I tried to use `math.select`
4. Running each Job per chunk, which turned out to be extra headaches to calculate coordinates and less efficient, since chunks werent that big
5. To support wrapping around, I tried **linear interpolation** between heights from very right side to first column on left side

## Attempt 2 - converting to a sphere
Naturally, watching Sebastian's tutorials and coding adventure videos, I also stumbled upon his planet generation videos. This gave me the idea of "wouldn't it be cool if the world was an actual sphere not flat map?". Of course, I watched a little bit of the [planet generation series](https://www.youtube.com/watch?v=QN39W020LqU) and started converting my code to work on a sphere map.
### Types of spheres
1. While I had the basic world generation for flat map working with grid like coordinates, I somehow had to map it to a sphere. The most traditional way to do this is using a [UV/Radial sphere](https://catlikecoding.com/unity/tutorials/procedural-meshes/uv-sphere/), but this would mean that tiles on the horizon would be way bigger than tiles near the poles.
2. Another approach, which I wanted to take was using the [Spherified cube](https://catlikecoding.com/unity/tutorials/procedural-meshes/cube-sphere/), which has more equal tile sizing, but still not good enough.
3. Finally, I considered using the [Fibonacci Sphere](https://youtu.be/CHWjNwvBRYs?si=tFCYxQXgRyMcCnd4&t=169), but by this time started reconsidering the planet approach and went back to flat map.

## Attempt 3 - back to flat map
After realizing that a flat map is easier and also wasting a lot of time in the process, I decided to at least get the continents working. This on its own had several attempts:
1. **Worley noise * Heightmap** - after looking at popular noise types, I saw [Worley noise](https://en.wikipedia.org/wiki/Worley_noise), which sort of represents continents. My idea was to generate both worley noise with point amount of how many continents you want to have, base heightmap and multiply those together. Of course also normalize the final value to 0-1, so they're not too low. Sadly, it mainly made the Simplex noise generated islands more pronounced.
2. **Minecraft terrain * Heightmap** - I watched a very detailed video on [how Minecraft generates terrain](https://www.youtube.com/watch?v=YyVAaJqYAfE&t=1078s) and tried implementing the "legacy stack" island generation code in a similar way as I did with Worley noise to make continents. Sadly, I got similar results and even worse - there were noticeable square patterns across the map!
3. **Just use a Heightmap** - Finally, I decided to just use the heightmap and call it a day :(

## Humidity
After generating heightmap, I also read up on [island map generation](http://www-cs-students.stanford.edu/~amitp/game-programming/polygon-map-generation/), from which I mainly got the idea on how to implement humidity for each tile. The principle is quite simple - for each land tile, count how far away it is from water. In code this meant to mark water tiles as 0, and go through adjacent tiles and add +1 if the neighbour has a value.

## Temperature
Temperature was even simpler - for each tile evaluate the base temperature based on latitude and subtract the ground height above water level. This meant that poles have cold climates, as well as high mountains, but with less snow on mountains near the horizon.

## Rendering
To display my heightmap, temperature and humidity effects I had to compute the `Mesh` and also compute a dynamic `Texture2D`. Both were heavy tasks, because for mesh rendering I had to account for LOD - where I had to calculate bigger and less triangles for other LOD, and for texturing I had to learn some **HLSL** to write my custom shader.
### Mesh generation
Out of the two, mesh generation was the hardest to get right. The main concept for LOD and stitching together different LOD level chunks comes from Sebastian's [Terrain Generation video](https://youtu.be/c2BUgXdjZkg?si=CJVV-fBdKOXxSmDF&t=164). Although there are many key differences comparing mine with his:
1. I used Unity Jobs to parallelize this process
2. The job goes through every Mesh's `Vertex` point. Unity Jobs strongly discourage writing to the same array index, so I had to take the largest index used, which turned out to be Vertex count
3. `Normals` are computed per **Normal point**, rather than computing a triangle normal and adding the value to all its vertices, which are later normalized. This was the main headache, as I had to account for each side and edge case separately
4. Since I am using Vertex count as index, I couldn't easily and efficiently assign `Triangles`, so a workaround was to create a MeshQuad struct, which held info about the 2 triangles and a flag whether the quad is actually in use
5. Since some Triangles are marked as unused, I had to a second `for loop` pass to convert them into actual triangle array, not to mention doing the same to Vertex and Normal points, as those had to be converted from `float3` to `Vector3`
### Texturing
While not as hard as mesh generation, learning HLSL was somewhat confusing. Initially I tried using **ShaderGraph** as it provided a visual interface to create HLSL shaders. Sadly ShaderGraph doesn't allow passing array data and has several limitations for my use case. In the end I did a lot of googling, and of course with help of ChatGPT, managed to put together a HLSL shader that flattens out the water, colors it based on depth and applies textures based on terrain.<br/>
Some annoyances of course:
1. To pass the height, temperature, humidity data I had to convert the arrays into a `Texture2D`, since shaders don't accept arrays (?)
2. For some unknown reason, I couldn't get the height in texture to match up with mesh height, so decided to use actual `Object Space` height as height :/
3. Debugging HLSL code sure is fun when it barely displays errors /s
4. Sadly there isn't an easy way to get shadows on the texture, so for now I have some glitchy shadows and lightness using **Lambertian reflection**