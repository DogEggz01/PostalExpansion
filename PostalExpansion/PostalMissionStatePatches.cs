using HarmonyLib;

namespace PostalExpansion
{
	[HarmonyPatch(typeof(PlayerMissions), "AcceptMission")]
	internal static class PostalMissionAcceptancePatch
	{
		private static void Postfix(Mission mission)
		{
			UrgentExpressMail.MissionAccepted(mission);
			GoldenDeliveryMissions.MissionAccepted(mission);
			RegisteredLetterMissions.MissionAccepted(mission);
			AnonymousLetterMissions.MissionAccepted(mission);
		}
	}

	[HarmonyPatch(typeof(SaveLoadManager), "SaveModData")]
	internal static class PostalMissionSavePatch
	{
		private static void Prefix()
		{
			UrgentExpressMail.SavePersistentState();
			GoldenDeliveryMissions.SavePersistentState();
			RegisteredLetterMissions.SavePersistentState();
			AnonymousLetterMissions.SavePersistentState();
			SpecialMailHistory.SavePersistentState();
		}
	}

	[HarmonyPatch(typeof(SaveLoadManager), "LoadModData")]
	internal static class PostalMissionLoadPatch
	{
		private static void Postfix()
		{
			UrgentExpressMail.LoadPersistentState();
			GoldenDeliveryMissions.LoadPersistentState();
			RegisteredLetterMissions.LoadPersistentState();
			AnonymousLetterMissions.LoadPersistentState();
			SpecialMailHistory.LoadPersistentState();
			LetterMissionDebug.Reset();
		}
	}

	[HarmonyPatch(typeof(StartMenu), "StartNewGame")]
	internal static class PostalMissionNewGamePatch
	{
		private static void Prefix()
		{
			UrgentExpressMail.ResetRuntimeState();
			GoldenDeliveryMissions.ResetRuntimeState();
			RegisteredLetterMissions.ResetRuntimeState();
			AnonymousLetterMissions.ResetRuntimeState();
			SpecialMailHistory.ResetRuntimeState();
			LetterMissionDebug.Reset();
		}
	}
}
