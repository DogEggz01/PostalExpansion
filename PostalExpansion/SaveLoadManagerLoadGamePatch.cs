using HarmonyLib;

namespace PostalExpansion
{
	[HarmonyPatch(typeof(SaveLoadManager), "LoadGame")]
	internal static class SaveLoadManagerLoadGamePatch
	{
		private static void Prefix()
		{
			PrefabsDirectory directory = PrefabsDirectory.instance;
			Letter.EnsureRegistered(directory);
			PostalExpressMail.EnsureRegistered(directory);
		}
	}
}
