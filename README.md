# Mini.RegionInstall

This mod allows you to pre-install a server region into your Among Us.

This can be useful when creating a modpack and installing a custom server for all users of your mod.

## Installation

If you already received a copy as part of a modpack, go to the Configuration section. Otherwise,

1. Download a copy from the [Releases](https://github.com/miniduikboot/Mini.RegionInstall/releases) page. If you do not use Reactor, use Mini.RegionInstall-NoReactor.dll. Otherwise, use Mini.RegionInstall.dll
2. Install [BepInEx](https://docs.reactor.gg/quick_start/install_bepinex) according to this guide
3. Copy the dll to `BepInEx/plugins`
4. Run Among Us once to generate a default configuration
5. Open `BepInEx/config/at.duikbo.regioninstall.cfg` in your favorite text editor. Read the next section on how to configure it.

To ship this mod with your other mods, include the plugin dll and the config file

## Configuration

### Regions

This value contains the contents of a regionInfo.json or just the array of regions in this file. To generate such a file, go to the [Impostor site](https://impostor.github.io/Impostor/), fill in your server address, the port and the desired name and press "Download Server File". Copy the contents of the file you just downloaded and set it as this config value.

Note that after the first start BepInEx will escape all quotes (`"`) in the value, this is expected and not harmful.

### RemoveRegions

This option allows you to remove regions by name from a config file. This is only useful if you're renaming your server: if you change other elements of your config but keep the name the same it is enough to put the new version in the Regions array.

Note that the standard Among Us regions cannot be removed in this way.

## License

Mini.RegionInstall is licensed under the [GNU General Public License v3.0](<https://tldrlegal.com/license/gnu-general-public-license-v3-(gpl-3)>)

To help with GPL compliance, the plugin dll contains a zip with its own source code. Note however that not all viewers accept this file.

## Support (next to Github issues)

Discord: miniduikboot#2965
Email: mini at duikbo dot at
[Carrier Pigeon](https://en.wikipedia.org/wiki/IPoAC): Discontinued due to a lack of reliable delivery.
