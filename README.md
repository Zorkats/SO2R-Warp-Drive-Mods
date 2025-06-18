# Star Ocean: The Second Story R - Warp Drive QoL Mods
A collection of mods that will hopefully help someone with the same issues or nitpicks I had with this wonderful game.
This is a collection of essential Quality of Life (QoL) patches for **Star Ocean: The Second Story R**. This all-in-one mod addresses several common frustrations and adds highly requested features to improve the player experience, from customizable movement speed to better UI information.

## Features

* **Movement Speed Multiplier (Warp Drive):** Customize your field movement and running speed. Speed up exploration and get around the map faster.
* **Adjustable Aggro Range:** Control how easily enemies can detect you. Lower the value to explore without constant interruptions, or raise it for a challenge.
* **Pause on Focus Loss:** The game will automatically pause itself whenever the game window loses focus, preventing you from being attacked while looking at another screen.
* **"Now Playing" BGM Info Display:** A sleek UI element appears when a new song starts, showing its title, composer, and other details.
* **Customizable Music Database:** The BGM Info feature is powered by an external `BgmNames.yaml` file, allowing users to add or correct song metadata without touching the mod itself.

## Requirements

* **BepInEx for IL2CPP:** This mod requires a working installation of BepInEx **6**, *THIS GAME DO NOT WORK WITH 5*. Please follow the official installation instructions.

## Installation

1.  Click on the **[Releases](https://github.com/Zorkats/SO2R-Warp-Drive-Mods/releases)** page on the right-hand side of this repository.
2.  Download the latest `.zip` file.
3.  Unzip the contents. You will find two `.dll` files, one of them reads the `.yaml` with the information about the OST.
4.  Move the mod's `.dll` file (e.g., `SO2R_QoL_Mods.dll`) into your `BepInEx/plugins/` folder.
5.  Move the `BgmNames.yaml` file into your `BepInEx/config/` folder.
6.  Run the game once. This will generate the main configuration file in the config folder.

## Configuration

All features can be enabled, disabled, or tuned by editing the configuration file located at: `BepInEx/config/com.zorkats.so2r_qol.cfg`

#### General

* `Pause On Focus Loss` \[true/false]: Toggles the auto-pause feature.

#### BGM Info

* `Enable` \[true/false]: Toggles the "Now Playing" UI on or off.
* `Show Once Per Session` \[true/false]: If true, BGM info is shown only the first time a track plays. If false, it shows every time the track changes.

#### Gameplay

* `Enable Movement Speed Multiplier` \[true/false]: Toggles the speed multiplier.
* `Movement Speed Multiplier` \[Default: 1.5]: Sets the speed multiplier. 1.0 is normal game speed, 2.0 is double speed. In my own recommendation I would say 1.75 is the best.

* `Enable Aggro Range Multiplier` \[true/false]: Toggles the enemy detection multiplier.
* `Aggro Range Multiplier` \[Default: 0.5]: Multiplier for enemy vision and proximity detection. 0.5 is half the normal range, 2.0 is double. Setting it to 0 makes you effectively invisible. 3 is probably my favorite value here.


## Future Plans & TODO List

This mod is in active development. Here are some of the features and ideas planned for future updates. Feel free to suggest more by opening an "Issue" on this GitHub repository!


Difficulty & Gameplay Modifiers

    Configurable multipliers for enemy stats (HP, ATK, DEF, etc.).
    Adjustable rates for EXP, Fol, SP, and BP gain.
    Ability to change the damage dealt by and/or received by the player party.

UI & Information Enhancements

    An option to display enemy HP bars in combat.
    Show numeric values for party HP/MP on the field HUD.

Field & Skill Adjustments

    Configurable success rates for Item Creation and Field Skills (like Pickpocketing).

System Features

    A configurable auto-save feature (e.g., save every 5 minutes or on scene change).
    Investigate a "Save Anywhere" function.
