// <copyright file="RegionInfoConverter.cs" company="miniduikboot">
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
	using System.Text.Json;
	using System.Text.Json.Serialization;

	/**
	 * <summary>
	 * This class deserializes IRegionInfo objects.
	 * </summary>
	 *
	 * Because there are two implementations of <see cref="IRegionInfo"/>, <see cref="DnsRegionInfo"/> and <see cref="StaticRegionInfo"/>, we need to manually handle them.
	 */
	internal class RegionInfoConverter : JsonConverter<IRegionInfo>
	{
		/// <inheritdoc/>
		public override IRegionInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartObject)
			{
				throw new JsonException();
			}

			Dictionary<string, string> stringValues = new Dictionary<string, string>();
			Dictionary<string, ushort> intValues = new Dictionary<string, ushort>();
			Dictionary<string, bool> boolValues = new Dictionary<string, bool>();

			while (reader.Read())
			{
				if (reader.TokenType == JsonTokenType.EndObject)
				{
					return Assemble(stringValues, intValues, boolValues);
				}
				else if (reader.TokenType == JsonTokenType.PropertyName)
				{
					string? propName = reader.GetString();
					_ = reader.Read();

					if (propName == "Port" || propName == "TranslateName")
					{
						ushort val = reader.GetUInt16();
						intValues.Add(propName, val);
					}
					else if (propName == "UseDtls")
					{
						bool val = reader.GetBoolean();
						boolValues.Add(propName, val);
					}
					else if (propName != null)
					{
						string? val = reader.GetString();
						if (val != null)
						{
							stringValues.Add(propName, val);
						}
					}
				}
				else
				{
					throw new JsonException($"RIC: Expected PropertyName or EndObject, got {reader.TokenType}");
				}
			}

			throw new JsonException("RIC: Could no longer read properties");
		}

		/// <inheritdoc/>
		public override void Write(Utf8JsonWriter writer, IRegionInfo value, JsonSerializerOptions options)
		{
			throw new NotImplementedException();
		}

		private static IRegionInfo? Assemble(Dictionary<string, string> stringValues, Dictionary<string, ushort> intValues, Dictionary<string, bool> boolValues)
		{
			if (!stringValues.TryGetValue("$type", out string type))
			{
				return null;
			}

			switch (type)
			{
				case "DnsRegionInfo, Assembly-CSharp":
					stringValues.TryGetValue("Fqdn", out string? fqdn);
					stringValues.TryGetValue("DefaultIp", out string? defaultIp);
					stringValues.TryGetValue("Name", out string? name);
					intValues.TryGetValue("Port", out ushort port);
					intValues.TryGetValue("TranslateName", out ushort translateName);
					boolValues.TryGetValue("UseDtls", out bool useDtls); // Defaults to false

					// HACK: Unhollower is unable to deal with inheritance properly, so you need special code to deal with it.
					// Simplify when https://github.com/knah/Il2CppAssemblyUnhollower/issues/33 is resolved.
					return new DnsRegionInfo(fqdn, name, (StringNames)translateName, defaultIp, port, useDtls).Duplicate();

				case "StaticRegionInfo, Assembly-CSharp":
					// TODO implement
					break;
			}

			return null;
		}

		private static IRegionInfo? AddLocalhost()
		{
			return new DnsRegionInfo("localhost", "Localhost", StringNames.NoTranslation, "127.0.0.1", 22023, false).Cast<IRegionInfo>();
		}
	}
}
