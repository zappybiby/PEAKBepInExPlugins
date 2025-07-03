# Configurable Storm Controller

This BepInEx plugin gives you complete control over the frequency of rain and snow storms in the game. You can make storms more or less frequent, or disable them entirely.

## Features
*   **Separate Controls:** Adjust the frequency of snow storms and rain storms independently.
*   **Full Frequency Range:** Make storms occur up to 5x more often, or make them extremely rare.
*   **Disable Storms:** Set the frequency multiplier to `0` to completely turn off a storm type.
*   **Live Configuration Reloading:** Change your settings while the game is running and see the effects without needing to restart!

---

## Installation

1.  Make sure you have BepInEx installed correctly. (https://github.com/BepInEx/BepInEx/releases -> 5.4, x64 -> extract to where PEAK.exe is -> run game once to generate Bepinex files)
2.  Download the `ConfigurableStormController.dll` file.
3.  Place the `.dll` file inside your `[Game Directory]\BepInEx\plugins\` folder.

## Configuration

### Step 1: First-Time Setup

Run the game with the plugin installed. It will make a config file for you.

### Step 2: Edit the Config File

1.  Navigate to your `[Game Directory]\BepInEx\config\` folder.
2.  Open the file `com.example.configurablestormcontroller.cfg` with any text editor (like Notepad).
3.  You will see the following settings:
    ```ini
    [1. General]
    EnablePlugin = true

    [2. Snow Storm]
    EnableSnowModification = true
    SnowFrequencyMultiplier = 0.5

    [3. Rain Storm]
    EnableRainModification = true
    RainFrequencyMultiplier = 1
    ```

### Understanding the Settings

*   `Enable...Modification`: Set this to `false` if you want a storm type to behave normally (vanilla), ignoring the multiplier.
*   `...FrequencyMultiplier`: This is the main setting for controlling how often storms happen.
    *   **Value > 1.0**: More frequent storms. (e.g., `2.0` is twice as often).
    *   **Value = 1.0**: Normal game frequency.
    *   **Value < 1.0**: Less frequent storms. (e.g., `0.5` is half as often, meaning the calm period between storms is twice as long).
    *   **Value = 0.0**: Disables this type of storm completely.

## Live Reloading (Changing Settings Mid-Game)

You do not need to restart the game to apply changes!

1.  Launch the game and load into a session.
2.  Alt-Tab out of the game and edit the `.cfg` file as described above.
3.  **Save the file.**
4.  The plugin will automatically detect the changes and apply them. The next time a storm cycle is calculated, it will use your new settings.

---

> **Important: Enable the BepInEx Console**
>
> To see if the plugin is working correctly and to get feedback on live reloading, it is highly recommended that you enable the BepInEx console.
>
> 1.  Go to `[Game Directory]\BepInEx\config\`.
> 2.  Open `BepInEx.cfg`.
> 3.  Find the `[Logging.Console]` section.
> 4.  Set `Enabled = true`.
> 5.  Save the file.
>
> Now, a console window will appear when you launch the game. You will see a message confirming that the "Configurable Storm Controller" has loaded, and you will see messages when you save changes to the config file live.
