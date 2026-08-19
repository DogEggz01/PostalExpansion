using HarmonyLib;

namespace PostalExpansion
{
	[HarmonyPatch(typeof(MissionListUI), "ChangePage")]
	internal static class MissionListUiChangePagePatch
	{
		private static bool Prefix(MissionListUI __instance, int pageChange)
		{
			return !PostalMissionUi.TryChangePage(__instance, pageChange);
		}
	}
}
