// <copyright file="RegionInstallPlugin.cs" company="miniduikboot">
// This file is part of Mini.RegionInstaller.
//
// Mini.RegionInstaller is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Mini.RegionInstaller is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Mini.RegionInstaller.  If not, see https://www.gnu.org/licenses/
// </copyright>

namespace Mini.RegionInstall
{
	using System;
	using BepInEx;
	using BepInEx.Configuration;
	using BepInEx.IL2CPP;
	using Newtonsoft.Json;
	using Reactor;

	/**
	 * <summary>
	 * Plugin that installs user specified servers into the region file.
	 * </summary>
	  */
	[BepInPlugin(Id)]
	[BepInProcess("Among Us.exe")]
	[BepInDependency(ReactorPlugin.Id)]
	public class RegionInstallPlugin : BasePlugin
	{
		private const string Id = "at.duikbo.regioninstall";

		/**
		 * <summary>
		 * Load the plugin and install the servers.
		 * </summary>
		 */
		public override void Load()
		{
			this.Log.LogInfo("Starting Mini.RegionInstall");
			ConfigEntry<bool>? addDefaultRegions = this.Config.Bind(
				"General",
				"AddDefaultRegions",
				true,
				"Set to true if you want to add the default Among Us regions to the menu");
			ConfigEntry<string>? regions = this.Config.Bind(
				"General",
				"Regions",
				"{\"CurrentRegionIdx\":0,\"Regions\":[]}",
				"Create an array of regions you want to add/update. To create this array, go to https://impostor.github.io/Impostor/ and put the Regions array from the server file in here");

			if (addDefaultRegions?.Value == true)
			{
				this.Log.LogInfo("Adding DefaultRegions");
				this.AddRegions(ServerManager.DefaultRegions);
			}

			if (regions != null && regions.Value.Length != 0)
			{
				this.Log.LogInfo("Adding User Regions");
				this.AddRegions(this.ParseRegions(regions.Value));
			}
		}

		/**
		 * <summary>
		 * Add an array of regions to AU's <see cref="ServerManager"/>.
		 * </summary>
		 */
		private void AddRegions(IRegionInfo[] regions)
		{
			ServerManager serverMngr = DestroyableSingleton<ServerManager>.Instance;
			IRegionInfo? currentRegion = serverMngr.CurrentRegion;
			this.Log.LogInfo($"Adding {regions.Length} regions");
			foreach (IRegionInfo region in regions)
			{
				serverMngr.AddOrUpdateRegion(region);
			}

			// AU remembers the previous region that was set, so we need to restore it
			serverMngr.SetRegion(currentRegion);
		}

		private IRegionInfo[] ParseRegions(string regions)
		{
			switch (regions[0])
			{
				// The entire JsonServerData
				case '{':
					this.Log.LogInfo("Loading server data");

					// This is the only JsonConvert.DeserializeObject generic that is available in Among Us at the moment.
					ServerManager.JsonServerData? result = JsonConvert.DeserializeObject<ServerManager.JsonServerData>(
						regions,
						new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
					if (result == null)
					{
						this.Log.LogError("Could not parse configured regions");
						return Array.Empty<IRegionInfo>();
					}

					return result.Regions;

				// Only the IRegionInfo array
				case '[':
					this.Log.LogInfo("Loading region array");

					// Sadly AU does not have a Generic that parses IRegionInfo[] directly, so instead we wrap the array into a JsonServerData structure.
					return this.ParseRegions($"{{\"CurrentRegionIdx\":0,\"Regions\":{regions}}}");
				default:
					this.Log.LogError("Could not detect format of configured regions");
					return Array.Empty<IRegionInfo>();
			}
		}
	}
}
