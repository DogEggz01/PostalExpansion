using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace PostalExpansion
{
	internal static class UrgentExpressMail
	{
		internal const int RequiredReputation = 6;
		internal const float RewardMultiplier = 2f;

		private const float DueDayBaseSpeed = 3.2f;
		private const string NamePrefix = "Urgent ";
		private const string ActiveMissionDataKey = "PostalExpansion.UrgentExpressMail.Active.v1";
		private const string ClaimedOfferDataKey = "PostalExpansion.UrgentExpressMail.Claimed.v1";

		private static ConditionalWeakTable<Mission, Marker> urgentMissions =
			new ConditionalWeakTable<Mission, Marker>();
		private static readonly HashSet<string> claimedOffers = new HashSet<string>();

		internal static bool IsUrgent(Mission mission)
		{
			return mission != null && urgentMissions.TryGetValue(mission, out _);
		}

		internal static bool CanOfferAt(Port origin)
		{
			if (origin == null)
			{
				return false;
			}

			PruneClaimsForCurrentDay();
			return !claimedOffers.Contains(GetClaimKey(GameState.day, origin.portIndex));
		}

		internal static void MarkGenerated(Mission mission)
		{
			if (!IsExpressMission(mission) || IsUrgent(mission))
			{
				return;
			}

			Good expressGood = mission.goodPrefab.GetComponent<Good>();
			if (expressGood == null)
			{
				return;
			}

			MarkRuntime(mission);
			mission.totalPrice = Mathf.RoundToInt(mission.totalPrice * RewardMultiplier);
			mission.dueDay = MailDueDateCalculator.Calculate(
				mission.originPort,
				mission.destinationPort,
				expressGood,
				DueDayBaseSpeed);
			mission.pricePerKm = mission.totalPrice / mission.distance;
		}

		internal static void MissionAccepted(Mission mission)
		{
			if (!IsUrgent(mission) ||
				mission.originPort == null ||
				mission.missionIndex < 0 ||
				PlayerMissions.missions == null ||
				mission.missionIndex >= PlayerMissions.missions.Length ||
				PlayerMissions.missions[mission.missionIndex] != mission)
			{
				return;
			}

			PruneClaimsForCurrentDay();
			claimedOffers.Add(GetClaimKey(GameState.day, mission.originPort.portIndex));
		}

		internal static void SavePersistentState()
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
					if (!IsUrgent(mission) ||
						mission.originPort == null ||
						mission.destinationPort == null)
					{
						continue;
					}

					activeEntries.Add(string.Join(",",
						i,
						mission.originPort.portIndex,
						mission.destinationPort.portIndex));
				}
			}

			GameState.modData[ActiveMissionDataKey] = string.Join(";", activeEntries);

			PruneClaimsForCurrentDay();
			var claims = new List<string>(claimedOffers);
			claims.Sort(StringComparer.Ordinal);
			GameState.modData[ClaimedOfferDataKey] = string.Join(";", claims);
		}

		internal static void LoadPersistentState()
		{
			ResetRuntimeState();
			if (GameState.modData == null)
			{
				return;
			}

			if (GameState.modData.TryGetValue(ClaimedOfferDataKey, out string claimData))
			{
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

			if (PlayerMissions.missions == null ||
				!GameState.modData.TryGetValue(ActiveMissionDataKey, out string missionData))
			{
				return;
			}

			foreach (string entry in missionData.Split(
				new[] { ';' },
				StringSplitOptions.RemoveEmptyEntries))
			{
				string[] fields = entry.Split(',');
				if (fields.Length != 3 ||
					!int.TryParse(fields[0], out int slot) ||
					!int.TryParse(fields[1], out int originIndex) ||
					!int.TryParse(fields[2], out int destinationIndex) ||
					slot < 0 ||
					slot >= PlayerMissions.missions.Length)
				{
					continue;
				}

				Mission mission = PlayerMissions.missions[slot];
				if (IsExpressMission(mission) &&
					mission.originPort != null &&
					mission.destinationPort != null &&
					mission.missionIndex == slot &&
					mission.originPort.portIndex == originIndex &&
					mission.destinationPort.portIndex == destinationIndex)
				{
					MarkRuntime(mission);
				}
			}
		}

		internal static void ResetRuntimeState()
		{
			urgentMissions = new ConditionalWeakTable<Mission, Marker>();
			claimedOffers.Clear();
		}

		private static void MarkRuntime(Mission mission)
		{
			urgentMissions.GetValue(mission, _ => new Marker());
			if (string.IsNullOrEmpty(mission.missionName))
			{
				mission.missionName = NamePrefix.TrimEnd();
			}
			else if (!mission.missionName.StartsWith(NamePrefix, StringComparison.Ordinal))
			{
				mission.missionName = NamePrefix + mission.missionName;
			}
		}

		private static bool IsExpressMission(Mission mission)
		{
			if (mission == null || mission.goodPrefab == null)
			{
				return false;
			}

			SaveablePrefab saveable = mission.goodPrefab.GetComponent<SaveablePrefab>();
			return PostalMail.IsExpressMail(saveable);
		}

		private static string GetClaimKey(int day, int originPortIndex)
		{
			return day + "," + originPortIndex;
		}

		private static void PruneClaimsForCurrentDay()
		{
			string currentDayPrefix = GameState.day + ",";
			claimedOffers.RemoveWhere(
				claim => !claim.StartsWith(currentDayPrefix, StringComparison.Ordinal));
		}

		private sealed class Marker
		{
		}
	}
}
