using System.Collections.Generic;

namespace PostalExpansion
{
	internal static class AnonymousLetterMissions
	{
		internal const int RequiredReputation = 8;
		internal const float DueDayBaseSpeed =
			RegisteredLetterMissions.DueDayBaseSpeed;
		internal const float ReputationMultiplier = 10f;
		internal const int FixedGoldReward = 5;
		internal const int OneDayLateGoldReward = 1;
		internal const int SeverelyLatePenalty = -8000;

		private static readonly LetterMissionSaveKeys SaveKeys =
			new LetterMissionSaveKeys(
				"PostalExpansion.AnonymousLetter.Active.v1",
				"PostalExpansion.AnonymousLetter.Claimed.v1");
		// Pirate Hideout used Registered Letter persistence before version 0.5.13.
		private static readonly LetterMissionSaveKeys LegacyPirateHideoutSaveKeys =
			new LetterMissionSaveKeys(
				"PostalExpansion.RegisteredLetter.Active.v1",
				"PostalExpansion.RegisteredLetter.Claimed.v1");

		private static readonly LetterMissionSet<AnonymousLetterMissionDefinition>
			Missions = new LetterMissionSet<AnonymousLetterMissionDefinition>(
				"Anonymous Letter",
				AnonymousLetterMissionRegistry.All,
				SaveKeys,
				RequiredReputation,
				DueDayBaseSpeed,
				FixedGoldReward,
				destination => "Anonymous letter");

		internal static void AddMissions(Port origin, List<Mission> missions)
		{
			Missions.AddMissions(origin, missions);
		}

		internal static int GetGoldReward(int daysLate)
		{
			if (daysLate <= 0)
			{
				return FixedGoldReward;
			}

			return daysLate == 1 ? OneDayLateGoldReward : 0;
		}

		internal static List<string> GetDebugStatusLines()
		{
			return Missions.GetDebugStatusLines();
		}

		internal static bool TryGetDefinition(
			Mission mission,
			out AnonymousLetterMissionDefinition definition)
		{
			return Missions.TryGetDefinition(mission, out definition);
		}

		internal static void MissionAccepted(Mission mission)
		{
			Missions.MissionAccepted(mission);
		}

		internal static void MissionDelivered(Mission mission)
		{
			Missions.MissionDelivered(mission);
		}

		internal static void SavePersistentState()
		{
			Missions.SavePersistentState();
		}

		internal static void LoadPersistentState()
		{
			Missions.LoadPersistentState(LegacyPirateHideoutSaveKeys);
		}

		internal static void ResetRuntimeState()
		{
			Missions.ResetRuntimeState();
		}
	}
}
