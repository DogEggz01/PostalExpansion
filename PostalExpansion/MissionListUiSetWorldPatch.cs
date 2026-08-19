using HarmonyLib;

namespace PostalExpansion
{
	[HarmonyPatch(typeof(MissionListUI), "SetWorldMissions")]
	internal static class MissionListUiSetWorldPatch
	{
		private static void Postfix(MissionListUI __instance)
		{
			if (GameState.inPortMissionList)
			{
				PostalMissionUi.SetMailMissions(false);
			}
		}
	}
}
