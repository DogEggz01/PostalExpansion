using HarmonyLib;

namespace PostalExpansion
{
	[HarmonyPatch(typeof(PrefabsDirectory), "Start")]
	internal static class PrefabsDirectoryStartPatch
	{
		private static void Postfix(PrefabsDirectory __instance)
		{
			Letter.EnsureRegistered(__instance);
			PostalExpressMail.EnsureRegistered(__instance);
		}
	}
}
