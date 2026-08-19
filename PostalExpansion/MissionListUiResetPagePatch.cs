using HarmonyLib;

namespace PostalExpansion
{
	[HarmonyPatch(typeof(MissionListUI), "ResetPage")]
	internal static class MissionListUiResetPagePatch
	{
		private static bool Prefix(MissionListUI __instance)
		{
			return !PostalMissionUi.TryResetPage(__instance);
		}
	}
}
