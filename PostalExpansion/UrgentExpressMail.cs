using System;
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

		private static readonly DailySpecialMissionState State =
			new DailySpecialMissionState(
				ActiveMissionDataKey,
				ClaimedOfferDataKey,
				SerializeActiveMission,
				MatchesSavedMission,
				ApplyMissionName);

		internal static bool IsUrgent(Mission mission)
		{
			return State.IsMarked(mission);
		}

		internal static bool CanOfferAt(Port origin)
		{
			return State.CanOfferAt(origin);
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
			State.MissionAccepted(mission);
		}

		internal static void SavePersistentState()
		{
			State.SavePersistentState();
		}

		internal static void LoadPersistentState()
		{
			State.LoadPersistentState();
		}

		internal static void ResetRuntimeState()
		{
			State.ResetRuntimeState();
		}

		private static void MarkRuntime(Mission mission)
		{
			State.Mark(mission);
			ApplyMissionName(mission);
		}

		private static void ApplyMissionName(Mission mission)
		{
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

		private static string SerializeActiveMission(int slot, Mission mission)
		{
			return string.Join(",",
				slot,
				mission.originPort.portIndex,
				mission.destinationPort.portIndex);
		}

		private static bool MatchesSavedMission(
			Mission mission,
			string[] fields)
		{
			return fields.Length == 3 &&
				int.TryParse(fields[1], out int originIndex) &&
				int.TryParse(fields[2], out int destinationIndex) &&
				IsExpressMission(mission) &&
				mission.originPort.portIndex == originIndex &&
				mission.destinationPort.portIndex == destinationIndex;
		}
	}
}
