using HarmonyLib;

namespace PostalExpansion
{
	[HarmonyPatch(typeof(MissionListUI), "DisplayMissions")]
	internal static class MissionListUiDisplayMissionsPatch
	{
		private static void Postfix(MissionListUI __instance)
		{
			PostalMissionUi.UpdateMissionHighlights(__instance);
		}
	}
}
