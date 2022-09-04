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
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using BepInEx;
	using BepInEx.Configuration;
	using BepInEx.Unity.IL2CPP;
	using HarmonyLib;
	using Newtonsoft.Json;
	using UnityEngine.SceneManagement;

	/**
	 * <summary>
	 * Plugin that installs user specified servers into the region file.
	 * </summary>
	 */
	[BepInAutoPlugin("at.duikbo.regioninstall")]
	[BepInProcess("Among Us.exe")]
	public partial class RegionInstallPlugin : BasePlugin
	{
		internal static BepInEx.Logging.ManualLogSource? Logger;

		private static ReadOnlyDictionary<string, IRegionInfo>? parsedRegions;

		public Harmony Harmony { get; } = new Harmony(Id);
		/**
		 * <summary>
		 * Load the plugin and install the servers.
		 * </summary>
		 */
		public override void Load()
		{
			Logger = this.Log;
			this.Log.LogInfo("Starting Mini.RegionInstall");
			this.Harmony.PatchAll();
			ConfigEntry<string>? regions = this.Config.Bind(
				"General",
				"Regions",
				"{\"CurrentRegionIdx\":0,\"Regions\":[]}",
				"Create an array of regions you want to add/update. To create this array, go to https://impostor.github.io/Impostor/ and put the Regions array from the server file in here");

			ConfigEntry<string>? removeRegions = this.Config.Bind(
				"General",
				"RemoveRegions",
				string.Empty,
				"Comma-seperated list of region names that should be removed.");

			// Register our regions when at the main menu to run after AU loads the server file
			SceneManager.add_sceneLoaded((Action<Scene, LoadSceneMode>)((scene, _) =>
			{
				if (scene.name == "MainMenu")
				{
					// Remove regions first in case the user accidentally also adds a region with the same name.
					if (removeRegions != null)
					{
						string[] rmRegions = removeRegions.Value.Split(",");
						this.Log.LogInfo($"Removing User Regions: \"{string.Join("\", \"", rmRegions)}\"");
						this.RemoveRegions(rmRegions);
					}

					if (regions != null && regions.Value.Length != 0)
					{
						this.Log.LogInfo("Adding User Regions");
						IRegionInfo[] parsed = this.ParseRegions(regions.Value);
						this.AddRegions(parsed);

						Dictionary<string, IRegionInfo> regionsDict = new Dictionary<string, IRegionInfo>();
						foreach (var region in parsed)
						{
							regionsDict[region.Name] = region;
						}
						parsedRegions = new ReadOnlyDictionary<string, IRegionInfo>(regionsDict);
					}
				}
			}));
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
				if (region == null)
				{
					this.Log.LogError("Could not add region");
				}
				else
				{
					if (currentRegion != null && region.Name.Equals(currentRegion.Name, StringComparison.OrdinalIgnoreCase))
					{
						currentRegion = region;
					}

					serverMngr.AddOrUpdateRegion(region);
				}
			}

			// AU remembers the previous region that was set, so we need to restore it
			if (currentRegion != null)
			{
				this.Log.LogDebug("Resetting previous region");
				serverMngr.SetRegion(currentRegion);
			}
		}

		private IRegionInfo[] ParseRegions(string regions)
		{
			this.Log.LogInfo($"Parsing {regions}");
			switch (regions[0])
			{
				// The entire JsonServerData
				case '{':
					this.Log.LogInfo("Loading server data");

					var result = JsonConvert.DeserializeObject<ServerManager.JsonServerData>(
						regions,
						new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

					foreach (IRegionInfo region in result.Regions)
					{
						this.Log.LogInfo($"Region \"{region.Name}\" @ {region.Servers[0].Ip}:{region.Servers[0].Port}");
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

		public static void CorrectCurrentRegion(ServerManager instance)
		{
			var region = instance.CurrentRegion;
			RegionInstallPlugin.Logger?.LogInfo($"Current region: {region.Name} ({region.Servers.Length} servers)");
			RegionInstallPlugin.Logger?.LogInfo($"Region \"{region.Servers[0].Name}\" @ {region.Servers[0].Ip}:{region.Servers[0].Port}");

			if (RegionInstallPlugin.parsedRegions != null && RegionInstallPlugin.parsedRegions.ContainsKey(region.Name))
			{
				instance.CurrentRegion = RegionInstallPlugin.parsedRegions[region.Name];

				RegionInstallPlugin.Logger?.LogInfo("Loading region from cache instead of from file");
				if (region.Servers[0].Port != instance.CurrentRegion.Servers[0].Port)
				{
					RegionInstallPlugin.Logger?.LogInfo($"Port corrected from {region.Servers[0].Port} to {instance.CurrentRegion.Servers[0].Port}");
				}
			}
		}

		private void RemoveRegions(string[] regionNames)
		{
			IEnumerable<IRegionInfo> newRegions = ServerManager.Instance.AvailableRegions.Where(
				(IRegionInfo r) => Array.FindIndex(regionNames, (string name) => name.Equals(r.Name, StringComparison.OrdinalIgnoreCase)) == -1);
			ServerManager.Instance.AvailableRegions = newRegions.ToArray();
		}
	}

	[HarmonyPatch(typeof(DnsRegionInfo), nameof(DnsRegionInfo.PopulateServers))]
	public static class DnsRegionInfoPatch
	{
		public static void Postfix(DnsRegionInfo __instance)
		{
			RegionInstallPlugin.Logger?.LogInfo($"DRI Populate Servers: {__instance.Fqdn}");
			foreach (var server in __instance.Servers)
			{
				RegionInstallPlugin.Logger?.LogInfo($"Configured server: {server.ToString()}");
			}
		}
	}

	[HarmonyPatch(typeof(ServerManager), nameof(ServerManager.ReselectServer))]
	public static class SMReselectPatch
	{
		public static void Prefix(ServerManager __instance)
		{
			RegionInstallPlugin.CorrectCurrentRegion(__instance);
		}

		public static void Postfix(ServerManager __instance)
		{
			var server = __instance.CurrentUdpServer;
			RegionInstallPlugin.Logger?.LogInfo($"Current server: {server.ToString()}");
		}
	}

	[HarmonyPatch(typeof(ServerManager), nameof(ServerManager.LoadServers))]
	public static class ServerManagerLoadServersPatch
	{
		public static void Postfix(ServerManager __instance)
		{
			RegionInstallPlugin.CorrectCurrentRegion(__instance);
			__instance.CurrentUdpServer = __instance.CurrentRegion.Servers[0];
		}
	}
}
