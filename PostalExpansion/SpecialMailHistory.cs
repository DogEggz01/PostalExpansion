using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace PostalExpansion
{
	internal static class SpecialMailHistory
	{
		private const string SaveKey =
			"PostalExpansion.SpecialMail.History.v1";

		private static ConditionalWeakTable<LoggedMission, Marker> historyKinds =
			new ConditionalWeakTable<LoggedMission, Marker>();

		internal static bool TryGetKind(
			LoggedMission mission,
			out SpecialMailKind kind)
		{
			if (mission != null && historyKinds.TryGetValue(mission, out Marker marker))
			{
				kind = marker.Kind;
				return true;
			}

			kind = SpecialMailKind.None;
			return false;
		}

		internal static void SavePersistentState()
		{
			if (GameState.modData == null)
			{
				GameState.modData = new Dictionary<string, string>();
			}

			var entries = new List<string>();
			LoggedMission[] missions = MissionLog.instance?.loggedMissions;
			if (missions != null)
			{
				for (int i = 0; i < missions.Length; i++)
				{
					LoggedMission mission = missions[i];
					if (TryGetKind(mission, out SpecialMailKind kind))
					{
						entries.Add(string.Join(",",
							i,
							(int)kind,
							mission.day,
							mission.goodIndex));
					}
				}
			}

			GameState.modData[SaveKey] = string.Join(";", entries);
		}

		internal static void LoadPersistentState()
		{
			ResetRuntimeState();
			LoggedMission[] missions = MissionLog.instance?.loggedMissions;
			if (missions == null ||
				GameState.modData == null ||
				!GameState.modData.TryGetValue(SaveKey, out string data))
			{
				return;
			}

			foreach (string entry in data.Split(
				new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
			{
				string[] fields = entry.Split(',');
				if (fields.Length != 4 ||
					!int.TryParse(fields[0], out int slot) ||
					!int.TryParse(fields[1], out int kindValue) ||
					!int.TryParse(fields[2], out int day) ||
					!int.TryParse(fields[3], out int goodIndex) ||
					slot < 0 ||
					slot >= missions.Length ||
					!Enum.IsDefined(typeof(SpecialMailKind), kindValue))
				{
					continue;
				}

				LoggedMission mission = missions[slot];
				var kind = (SpecialMailKind)kindValue;
				if (kind != SpecialMailKind.None &&
					mission != null &&
					mission.day == day &&
					mission.goodIndex == goodIndex)
				{
					Mark(mission, kind);
				}
			}

			MissionLog.instance.UpdateTexts();
		}

		internal static void ResetRuntimeState()
		{
			historyKinds = new ConditionalWeakTable<LoggedMission, Marker>();
		}

		internal static void Mark(LoggedMission mission, SpecialMailKind kind)
		{
			if (mission == null || kind == SpecialMailKind.None)
			{
				return;
			}

			historyKinds.Remove(mission);
			historyKinds.Add(mission, new Marker(kind));
		}

		private sealed class Marker
		{
			internal Marker(SpecialMailKind kind)
			{
				Kind = kind;
			}

			internal SpecialMailKind Kind { get; }
		}
	}

	[HarmonyPatch(typeof(MissionLog), nameof(MissionLog.AddToLog))]
	internal static class SpecialMailHistoryAddPatch
	{
		private const string TemporaryDestinationPrefix =
			"\u001FPostalExpansion:";

		private static void Prefix(
			MissionLog __instance,
			int goodIndex,
			ref string destinationName,
			out AddState __state)
		{
			__state = new AddState(
				SpecialMailDisplay.CurrentDeliveryKind,
				destinationName);
			LoggedMission top =
				__instance.loggedMissions != null && __instance.loggedMissions.Length > 0
					? __instance.loggedMissions[0]
					: null;
			if (top == null ||
				top.day != GameState.day ||
				top.goodIndex != goodIndex ||
				top.destinationName != destinationName)
			{
				return;
			}

			SpecialMailKind topKind = SpecialMailKind.None;
			SpecialMailHistory.TryGetKind(top, out topKind);
			if (topKind != __state.Kind)
			{
				destinationName =
					TemporaryDestinationPrefix + (int)__state.Kind + ":" + destinationName;
				__state.ForceSeparateEntry = true;
			}
		}

		private static void Postfix(MissionLog __instance, AddState __state)
		{
			LoggedMission top =
				__instance.loggedMissions != null && __instance.loggedMissions.Length > 0
					? __instance.loggedMissions[0]
					: null;
			if (top == null)
			{
				return;
			}

			if (__state.ForceSeparateEntry)
			{
				top.destinationName = __state.DestinationName;
			}
			if (__state.Kind != SpecialMailKind.None)
			{
				SpecialMailHistory.Mark(top, __state.Kind);
			}

			if (__state.ForceSeparateEntry || __state.Kind != SpecialMailKind.None)
			{
				__instance.UpdateTexts();
			}
		}

		internal struct AddState
		{
			internal AddState(SpecialMailKind kind, string destinationName)
			{
				Kind = kind;
				DestinationName = destinationName;
				ForceSeparateEntry = false;
			}

			internal SpecialMailKind Kind;
			internal string DestinationName;
			internal bool ForceSeparateEntry;
		}
	}

	[HarmonyPatch(typeof(MissionLog), nameof(MissionLog.UpdateTexts))]
	internal static class SpecialMailHistoryTextPatch
	{
		private static void Postfix(MissionLog __instance)
		{
			if (__instance.loggedMissions == null || __instance.texts == null)
			{
				return;
			}

			int count = Math.Min(
				__instance.loggedMissions.Length,
				__instance.texts.Length);
			for (int i = 0; i < count; i++)
			{
				LoggedMission mission = __instance.loggedMissions[i];
				if (!SpecialMailHistory.TryGetKind(
						mission,
						out SpecialMailKind kind))
				{
					continue;
				}

				__instance.texts[i].text =
					"day " + mission.day + ": " +
					SpecialMailDisplay.GetDisplayName(kind) + " to " +
					mission.destinationName + " (" +
					mission.deliveredGoodCount + ")";
			}
		}
	}
}
