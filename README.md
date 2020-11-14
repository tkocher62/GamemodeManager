# GamemodeManager

This is a plugin for my game server to allow for hot-swapping of external assemblies to run different special modes the server. Using the API within this plugin, other developers can create these special modes and have them registered into this one on server startup. This allows for this assembly to act as a manager, enabling and disabling modes based on different criteria.

### Runtime
When the server starts, the plugin will load all other special modes into its memory to manage.

![](https://github.com/tkocher62/GamemodeManager/blob/exiled/images/loaded.png)

### Commands
Upon running the main command, you are greeted with a lot of customizability and options.

![](https://github.com/tkocher62/GamemodeManager/blob/exiled/images/output.png)

The first command, `list`, allows you to view the list of gamemodes registered.

![](https://github.com/tkocher62/GamemodeManager/blob/exiled/images/list.png)

You can then use the `set` command to set the gamemode for next round.

![](https://github.com/tkocher62/GamemodeManager/blob/exiled/images/set.png)

For more automated usage, the plugin comes with several modes. For instance, the `vote` mode prompts every player to vote for what mode they want to play next at the end of each round. Furthermore, you can set a frequency on modes, causing them to run every `x` amount of rounds with normal rounds in between.

![](https://github.com/tkocher62/GamemodeManager/blob/exiled/images/setmode.png)

Finally, when the round restarts and a gamemode loads, it loads the custom made config file for that gamemode. This overrwites the server config to modify values and disable anything that may interfere with the gamemode.

![](https://github.com/tkocher62/GamemodeManager/blob/exiled/images/loading%20config.png)
