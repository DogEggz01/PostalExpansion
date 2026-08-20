using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PostalExpansion
{
	internal sealed class DailySpecialMissionState
	{
		private readonly string activeMissionDataKey;
		private readonly string claimedOfferDataKey;
		private readonly Func<int, Mission, string> serializeActiveMission;
		private readonly Func<Mission, string[], bool> matchesSavedMission;
		private readonly Action<Mission> onMissionRestored;

		private ConditionalWeakTable<Mission, Marker> markedMissions =
			new ConditionalWeakTable<Mission, Marker>();
		private readonly HashSet<string> claimedOffers =
			new HashSet<string>(StringComparer.Ordinal);

		internal DailySpecialMissionState(
			string activeMissionDataKey,
			string claimedOfferDataKey,
			Func<int, Mission, string> serializeActiveMission,
			Func<Mission, string[], bool> matchesSavedMission,
			Action<Mission> onMissionRestored)
		{
			this.activeMissionDataKey = activeMissionDataKey;
			this.claimedOfferDataKey = claimedOfferDataKey;
			this.serializeActiveMission = serializeActiveMission;
			this.matchesSavedMission = matchesSavedMission;
			this.onMissionRestored = onMissionRestored;
		}

		internal bool IsMarked(Mission mission)
		{
			return mission != null && markedMissions.TryGetValue(mission, out _);
		}

		internal void Mark(Mission mission)
		{
			if (mission != null)
			{
				markedMissions.GetValue(mission, _ => new Marker());
			}
		}

		internal bool CanOfferAt(Port origin)
		{
			if (origin == null)
			{
				return false;
			}

			PruneClaimsForCurrentDay();
			return !claimedOffers.Contains(
				GetClaimKey(GameState.day, origin.portIndex));
		}

		internal void MissionAccepted(Mission mission)
		{
			if (!IsMarked(mission) ||
				mission.originPort == null ||
				mission.missionIndex < 0 ||
				PlayerMissions.missions == null ||
				mission.missionIndex >= PlayerMissions.missions.Length ||
				PlayerMissions.missions[mission.missionIndex] != mission)
			{
				return;
			}

			PruneClaimsForCurrentDay();
			claimedOffers.Add(
				GetClaimKey(GameState.day, mission.originPort.portIndex));
		}

		internal void SavePersistentState()
		{
			if (GameState.modData == null)
			{
				GameState.modData = new Dictionary<string, string>();
			}

			var activeEntries = new List<string>();
			if (PlayerMissions.missions != null)
			{
				for (int i = 0; i < PlayerMissions.missions.Length; i++)
				{
					Mission mission = PlayerMissions.missions[i];
					if (!IsMarked(mission) ||
						mission.originPort == null ||
						mission.destinationPort == null)
					{
						continue;
					}

					string entry = serializeActiveMission(i, mission);
					if (!string.IsNullOrEmpty(entry))
					{
						activeEntries.Add(entry);
					}
				}
			}

			GameState.modData[activeMissionDataKey] =
				string.Join(";", activeEntries);
			PruneClaimsForCurrentDay();
			var claims = new List<string>(claimedOffers);
			claims.Sort(StringComparer.Ordinal);
			GameState.modData[claimedOfferDataKey] = string.Join(";", claims);
		}

		internal void LoadPersistentState()
		{
			ResetRuntimeState();
			if (GameState.modData == null)
			{
				return;
			}

			LoadClaims();
			LoadActiveMissions();
		}

		internal void ResetRuntimeState()
		{
			markedMissions = new ConditionalWeakTable<Mission, Marker>();
			claimedOffers.Clear();
		}

		private void LoadClaims()
		{
			if (!GameState.modData.TryGetValue(
					claimedOfferDataKey,
					out string claimData))
			{
				return;
			}

			string currentDayPrefix = GameState.day + ",";
			foreach (string claim in claimData.Split(
				new[] { ';' },
				StringSplitOptions.RemoveEmptyEntries))
			{
				if (claim.StartsWith(currentDayPrefix, StringComparison.Ordinal))
				{
					claimedOffers.Add(claim);
				}
			}
		}

		private void LoadActiveMissions()
		{
			if (PlayerMissions.missions == null ||
				!GameState.modData.TryGetValue(
					activeMissionDataKey,
					out string missionData))
			{
				return;
			}

			foreach (string entry in missionData.Split(
				new[] { ';' },
				StringSplitOptions.RemoveEmptyEntries))
			{
				string[] fields = entry.Split(',');
				if (fields.Length == 0 ||
					!int.TryParse(fields[0], out int slot) ||
					slot < 0 ||
					slot >= PlayerMissions.missions.Length)
				{
					continue;
				}

				Mission mission = PlayerMissions.missions[slot];
				if (mission != null &&
					mission.missionIndex == slot &&
					mission.originPort != null &&
					mission.destinationPort != null &&
					matchesSavedMission(mission, fields))
				{
					Mark(mission);
					onMissionRestored?.Invoke(mission);
				}
			}
		}

		private static string GetClaimKey(int day, int originPortIndex)
		{
			return day + "," + originPortIndex;
		}

		private void PruneClaimsForCurrentDay()
		{
			string currentDayPrefix = GameState.day + ",";
			claimedOffers.RemoveWhere(
				claim => !claim.StartsWith(
					currentDayPrefix,
					StringComparison.Ordinal));
		}

		private sealed class Marker
		{
		}
	}
}
