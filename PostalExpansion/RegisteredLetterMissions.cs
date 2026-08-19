using System.Collections.Generic;

namespace PostalExpansion
{
	internal static class RegisteredLetterMissions
	{
		internal const int RequiredReputation = 7;
		internal const float DueDayBaseSpeed = 3.0f;
		internal const float ReputationMultiplier = 8f;
		internal const int FixedGoldReward = 1;
		internal const int SeverelyLatePenalty = -5000;

		private static readonly LetterMissionSaveKeys SaveKeys =
			new LetterMissionSaveKeys(
				"PostalExpansion.RegisteredLetter.Active.v1",
				"PostalExpansion.RegisteredLetter.Claimed.v1");

		private static readonly LetterMissionSet<RegisteredLetterMissionDefinition>
			Missions = new LetterMissionSet<RegisteredLetterMissionDefinition>(
				"Registered Letter",
				RegisteredLetterMissionRegistry.All,
				SaveKeys,
				RequiredReputation,
				DueDayBaseSpeed,
				FixedGoldReward,
				destination =>
					"Registered Letter to " + destination.GetPortName());

		internal static void AddMissions(Port origin, List<Mission> missions)
		{
			Missions.AddMissions(origin, missions);
		}

		internal static List<string> GetDebugStatusLines()
		{
			return Missions.GetDebugStatusLines();
		}

		internal static bool TryGetDefinition(
			Mission mission,
			out RegisteredLetterMissionDefinition definition)
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
			Missions.LoadPersistentState();
		}

		internal static void ResetRuntimeState()
		{
			Missions.ResetRuntimeState();
		}
	}
}
