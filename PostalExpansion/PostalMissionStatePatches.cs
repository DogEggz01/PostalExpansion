using HarmonyLib;

namespace PostalExpansion
{
	[HarmonyPatch(typeof(PlayerMissions), "AcceptMission")]
	internal static class PostalMissionAcceptancePatch
	{
		private static void Postfix(Mission mission)
		{
			UrgentExpressMail.MissionAccepted(mission);
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
			RegisteredLetterMissions.SavePersistentState();
			AnonymousLetterMissions.SavePersistentState();
		}
	}

	[HarmonyPatch(typeof(SaveLoadManager), "LoadModData")]
	internal static class PostalMissionLoadPatch
	{
		private static void Postfix()
		{
			UrgentExpressMail.LoadPersistentState();
			RegisteredLetterMissions.LoadPersistentState();
			AnonymousLetterMissions.LoadPersistentState();
			LetterMissionDebug.Reset();
		}
	}

	[HarmonyPatch(typeof(StartMenu), "StartNewGame")]
	internal static class PostalMissionNewGamePatch
	{
		private static void Prefix()
		{
			UrgentExpressMail.ResetRuntimeState();
			RegisteredLetterMissions.ResetRuntimeState();
			AnonymousLetterMissions.ResetRuntimeState();
			LetterMissionDebug.Reset();
		}
	}
}
