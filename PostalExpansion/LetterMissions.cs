namespace PostalExpansion
{
	internal static class LetterMissions
	{
		internal static bool TryGetDefinition(
			Mission mission,
			out LetterMissionDefinition definition)
		{
			if (AnonymousLetterMissions.TryGetDefinition(
					mission,
					out AnonymousLetterMissionDefinition anonymousDefinition))
			{
				definition = anonymousDefinition;
				return true;
			}

			if (RegisteredLetterMissions.TryGetDefinition(
					mission,
					out RegisteredLetterMissionDefinition registeredDefinition))
			{
				definition = registeredDefinition;
				return true;
			}

			definition = null;
			return false;
		}

		internal static void MissionDelivered(Mission mission)
		{
			AnonymousLetterMissions.MissionDelivered(mission);
			RegisteredLetterMissions.MissionDelivered(mission);
		}

		internal static int GetFixedGoldReward(Mission mission)
		{
			return AnonymousLetterMissions.TryGetDefinition(mission, out _)
				? AnonymousLetterMissions.FixedGoldReward
				: RegisteredLetterMissions.FixedGoldReward;
		}

		internal static float GetReputationMultiplier(Mission mission)
		{
			return AnonymousLetterMissions.TryGetDefinition(mission, out _)
				? AnonymousLetterMissions.ReputationMultiplier
				: RegisteredLetterMissions.ReputationMultiplier;
		}

		internal static int GetSeverelyLatePenalty(Mission mission)
		{
			return AnonymousLetterMissions.TryGetDefinition(mission, out _)
				? AnonymousLetterMissions.SeverelyLatePenalty
				: RegisteredLetterMissions.SeverelyLatePenalty;
		}
	}
}
