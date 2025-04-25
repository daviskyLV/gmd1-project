# Roll-A-Ball implementation
Roll-a-Ball Unity project is a small introduction project to get you started with Unity, although by this point I already have some experience with it, so it was more like a refreshment for me.

Anyways, the project revolves around creating a player, an environment and some collectibles, that increase your score.
## Environment
First I made the environment - a basic `Plane` mesh to act as base ground, which I resized to make larger than the default one. I also copy pasted it around to create some platforms for the player to jump on.
## Lighting
Even though it wasn't in the tutorial, I wanted to have some disco and decided to make a quick script that changes the light's color based on time. In the code I simply used `Time.time` to get the elapsed time since game launch, got the millisecond part and used it as the Hue value for the light's color. 
## Materials
To not have such a bland scene, I decided to create a few color `Material`s - Green for ground, Yellow for collectibles and a shiny Purple one for the player. To make it shiny I adjusted the `Smoothness` and `Metallic` levels on the material.
## Player implementation
To create the player, I used a basic `Sphere` mesh with a `SphereCollider` and a `Rigidbody` for to make the player adhere to laws of physics. After that I made a `PlayerController` script in which I used Unity's `InputSystem` to get the player movement key inputs converted to `Vector2`. With the new InputSystem it was very easy to use the returned Vector2 value to add force to the player's `Rigidbody` to make it move.
## Camera tracking the player
To not lose sight of the player, I made a `CameraController` script, which in its `Start()` method measures the offset from the ball. In the subsequent `LateUpdate()` methods it realigns itself to ball's `Transform` position + the measured offset. I used `LateUpdate()` since it happens later in the frame render pipeline and is better suited for camera repositioning.
## Collectibles
To make the game more interesting, the collectibles were placed only on platforms and you have to jump to get them. Each of them awards 1 point. To achieve this, I made a `PrizeController` script, which has a `public static` score field and a `OnTriggerEnter()` method to detect when player touches the prize. To make the trigger method work, I marked the collectible's `Collider` as `IsTrigger = true`.
## Displaying score
To display the score, I made a quick UI text label, which has a script `ScoreDisplay`. The `ScoreDisplay` script simply updates the text label's text each frame to the `PrizeController.Score` field value.