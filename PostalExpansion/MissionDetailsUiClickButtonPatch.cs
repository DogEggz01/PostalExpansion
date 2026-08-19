using HarmonyLib;

namespace PostalExpansion
{
	[HarmonyPatch(typeof(MissionDetailsUI), "ClickButton")]
	internal static class MissionDetailsUiClickButtonPatch
	{
		private static bool Prefix(MissionDetailsUI __instance)
		{
			return !PostalMissionUi.TryHandlePortMissionAcceptance(__instance);
		}
	}
}
