using HarmonyLib;

namespace PostalExpansion
{
	[HarmonyPatch(typeof(ShipItem), nameof(ShipItem.UpdateLookText))]
	internal static class AnonymousLetterItemLookTextPatch
	{
		private static bool Prefix(ShipItem __instance)
		{
			if (__instance == null ||
				!PostalMail.IsRegisteredLetter(
					__instance.GetComponent<SaveablePrefab>()))
			{
				return true;
			}

			Good good = __instance.GetComponent<Good>();
			int missionIndex = good != null ? good.GetMissionIndex() : -1;
			if (missionIndex < 0 ||
				PlayerMissions.missions == null ||
				missionIndex >= PlayerMissions.missions.Length)
			{
				return true;
			}

			Mission mission = PlayerMissions.missions[missionIndex];
			if (!AnonymousLetterMissions.TryGetDefinition(mission, out _))
			{
				return true;
			}

			__instance.lookText = "due: " + mission.GetDueText();
			return false;
		}
	}
}
