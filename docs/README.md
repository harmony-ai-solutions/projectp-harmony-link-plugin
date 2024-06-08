# Harmony Link ProjectP-Plugin - Technical documentation

The Harmony Link ProjectP-Plugin is an Event-Layer Plugin for Project Harmony.AI's [Harmony Link AI-middleware](https://github.com/harmony-ai-solutions/harmony-link).

This is ProjectP-Plugin Harmony Link's technical documentation. For a more general overview of it's
capabilities, please check out this repo's 
[main page](https://github.com/harmony-ai-solutions/projectp-harmony-link-plugin).

##### - More Details on this repo's codebase coming soon -



##### Random notes on this Project for development:

- Runtime Environment is Unity Engine. Default C# Plugins might cause errors.
- Imports known to cause errors:
  - System.Diagnostics (Debug is already working through Unity's Debug library, so this import is never needed)
