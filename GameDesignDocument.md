# _Doodle Architect_ Game Design Document
Doodle with a pencil on your notebook to reach the next page! Be cautious of erasers, ink spills and other nasty stuff!

## Base info
**Genres**: 2D, drawing, puzzle <br/>
**Target audience**: Kids and people who want to play something simple for a little bit <br/>
**Uniqueness**: The player can draw the track to get through the level. They have to keep in mind the dynamics of the level, as the track can be impacted by ink spills, erasers and paper holes at runtime by enemies. The player can also doodle their own vehicle to traverse the track and avoid enemies.

## Gameplay
**Story**:
The player is a bored student or kid and has found a notebook to draw in. Starting pages (levels) are empty with some holes or ink spills, but later pages also have some weird symbols and drawings that no one, but the author of the notebook, can understand. The player understands these as enemies and it's better to avoid them. <br/>
Next to the notebook lays a pencil, which the player grabs and sets out on a mission to draw a path that would avoid all mistakes in the notebook and weird symbols from page 1 till end. <br/>
**Visual representation**:
The main background is a checkerboard notebook page, with symbols being related to math or foreign alphabeth, player's track and character resembles a pencil drawing. <br/>
**Core loop**:
On each level the player is given a limited pencil length that they can use to draw the path. They start on the left side of the page on the Y level that they left on the previous level. The player can also redraw the character to better fit the level. As levels progress, more and difficult obstacles are introduced and placed throughout the level.
**Objectives**:
Reach the right side of the page with the character (vehicle) and avoid obstacles. More points are earned the faster it's done and the more pencil is left over. <br/>
**End goal**:
Beat all the 30 levels and try to aim for the highest combined score.

## Technicalities
### Obstacles
**Ink spills**: look like ink splashes and slowly drip down the page. Going through them makes the objects slippery and can dampen velocity.<br/>
**Paper holes**: entering these is makes you lose a part (or all) of your body, based on what enters. Can cause a level fail. <br/>
**Math enemies**: player is bad at math, so math operations can fly at enemy based on proximity (eg. **-** sign acts like a flying eraser, but **+** like a flying shuriken that sticks to whatever it touches) <br/>
**Letter enemies**: every now and then spawn math enemies
### Drawing and Character
Player can draw a path anywhere on the page before pressing play. Cursor is controlled by joystick and a button is held down to draw. Player can also switch to eraser mode to remove their path. <br/>
Before pressing play, player must have a vehicle drawn, with one collider acting as a wheel. If the wheel is destroyed player has to restart the level. Player can move the character left or right by pressing the buttons.

## Milestones
**Milestone 1 - Minimum Viable Product**: The first milestone is to create a MVP. It should include:
1. Being able to draw a path
2. Progressing to the next level
3. Being able to draw player character
4. Implementing at least 1 obstacle (for example paper hole)
5. Ability to control the character

**Milestone 2 - Level generation & Obstacles**: The main objective of the second milestone is to implement the remaining obstacles and automatically generate obstacle placement based level and starting point. It encompasses:
1. Implementing rest of the obstacles that the player can encounter
2. Determining how many and what obstacles should be placed based on entry parameters
3. Fine tuning the levels so that they make sense and feel harder as you go on

**Milestone 3 - UI, Sound, polishing**: In the final milestone main emphasis is on UI, Sound and UX, including bug fixes. It includes:
1. Create a "pencil drawn" like UI for the game
2. Add missing sounds for drawing, collisions, enemy actions, etc.
3. Add some animation when transitioning between levels and starting the game
4. Leaderboards
5. Saving player characters
6. General bug fixes