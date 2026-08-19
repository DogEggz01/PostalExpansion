using HarmonyLib;

namespace PostalExpansion
{
	[HarmonyPatch(typeof(MissionListUI), "EnablePortMissionUI")]
	internal static class MissionListUiEnablePortPatch
	{
		private static void Prefix(MissionListUI __instance, ref Mission[] missions, PortDude dude)
		{
			PostalMissionUi.FilterInitialPortMissions(__instance, dude, ref missions);
		}

		private static void Postfix(MissionListUI __instance)
		{
			PostalMissionUi.EnsureMailButton(__instance);
			PostalMissionUi.RefreshPageCount(__instance);
		}
	}
}
