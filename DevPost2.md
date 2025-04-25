# Game dev blog post #2
The second milestone for the game was about giving powers to the player to somehow manipulate the terrain, making other civilization gods also cause mayhem, as well as civilizations fighting between each other. Of course, also UI!

## User Interfaces
A very important part of any game, so after finishing terrain generation I went on to work on Main Menu and Game Setup screens. I have used Godot to make some UI applications before, so my approach mainly attempted to emulate that workflow with Unity's component based system.

### Why are there 3 of them???
Unity has a lot of legacy code and systems, so it was kind of hard to understand what is meant to be used nowadays. I mostly sticked with what I saw on the menu when right clicking to add a `GameObject`. To create the menu buttons I just used the `Button` and was surprised at how **LITTLE** customization there was (at least comparing to Godot), for example, I wanted to make rounded corners and couldn't really find an easy setting for that. In the end I installed a "[Rounded Corners](https://github.com/kirevdokimov/Unity-UI-Rounded-Corners?tab=MIT-1-ov-file)" plugin to have my nice buttons :).

### Navigation implementation
Since on the arcade you can navigate only via a few buttons and a joystick, I had to implement my own navigation system. I did this in 3 parts - `MainMenuController`, `MenuButtonsController` and `GameSetupController`.
- MainMenuController - it sits under Canvas in Main Menu scene and acts as a bridge between `MenuButtonsController` and `GameSetupController` when opening/closing the game setup panel.
- MenuButtonsController - registers the movement while the game setup panel is closed and navigates between Play and Exit buttons. If the "Accept" key is pressed (mapped to `ButtonSouth`) then it gets the currently selected button and simulates a click.
- GameSetupController - performs a similar function as MenuButtonsController, except within the scope of Game Setup Panel. It also performs the animating in/out when opening and closing the panel. Animation works by simply rescaling the panel by evaluating an `AnimationCurve` which maps time passed to scale size.

### Sounds
Every UI needs at least some sound, so to have some feedback I added a few sound effects when scrolling between different settings in Game Setup panel and Main Menu buttons. Each button determines what sound it wants to play and then via reference to an `AudioSource` plays that sound when selected.

## Civilizations
There are max 10 civilizations, each with unique color. I made a Unit prefab for the civilizations, which has an array of available colors (materials) and when set up via `Setup()` method, it chooses the Unit's color based on its CivilizationId. The units try to find a nearby different civilization unit and then go towards them and attack. Movement is done via `Rigidbody.AddForce()` with direction towards enemy. To find the enemy targets a sphere check is used (`Physics.OverlapSphere`) with the "Unit" layer as mask.

The flags spawn more units of the same civilization every now and then, and all in all act similar to units themselves. Obviously, they're stationary.