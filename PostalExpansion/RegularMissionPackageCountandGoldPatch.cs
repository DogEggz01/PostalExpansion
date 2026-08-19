using System;
using HarmonyLib;
using UnityEngine;

namespace PostalExpansion
{
	[HarmonyPatch(typeof(IslandMissionOffice), "GenerateMailMission", new Type[]
	{
		typeof(Good),
		typeof(Port),
		typeof(float),
		typeof(int)
	})]
	internal static class RegularMissionPackageCountandGoldPatch
	{
		private static void Postfix(IslandMissionOffice __instance, float distance, Mission __result)
		{
			if (__result == null)
			{
				return;
			}

			int packageCount = Mathf.Clamp(
				PlayerReputation.GetRepLevel(__instance.GetPort().region),
				1,
				3);
			float distanceReward = distance * DebugMarketTracker.instance.missionDistanceFee;
			float packageMultiplier = packageCount * 0.5f + 0.5f;
			__result.goodCount = packageCount;
			__result.totalPrice = Mathf.RoundToInt(
				distanceReward *
				packageMultiplier *
				Plugin.RegularMailRewardMultiplier.Value);
			__result.pricePerKm = __result.totalPrice / __result.distance;
		}
	}
}
