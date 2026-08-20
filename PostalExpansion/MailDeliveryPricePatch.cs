using HarmonyLib;
using UnityEngine;

namespace PostalExpansion
{
	[HarmonyPatch(typeof(Mission), "GetDeliveryPrice")]
	internal static class MailDeliveryPricePatch
	{
		private static bool Prefix(Mission __instance, ref int __result)
		{
			if (GoldenDeliveryMissions.IsGolden(__instance))
			{
				int goldenDaysLate = Mathf.Max(
					0,
					GameState.day - __instance.dueDay);
				__result = goldenDaysLate == 0
					? __instance.totalPrice / Mathf.Max(1, __instance.goodCount)
					: 0;
				return false;
			}

			if (AnonymousLetterMissions.TryGetDefinition(__instance, out _))
			{
				int anonymousDaysLate = Mathf.Max(
					0,
					GameState.day - __instance.dueDay);
				__result = AnonymousLetterMissions.GetGoldReward(
					anonymousDaysLate);
				return false;
			}

			if (RegisteredLetterMissions.TryGetDefinition(__instance, out _))
			{
				__result = RegisteredLetterMissions.FixedGoldReward;
				return false;
			}

			if (!UrgentExpressMail.IsUrgent(__instance))
			{
				return true;
			}

			int daysLate = Mathf.Max(0, GameState.day - __instance.dueDay);
			float payoutFraction;
			if (daysLate == 0)
			{
				payoutFraction = 1f;
			}
			else if (daysLate == 1)
			{
				payoutFraction = 0.5f;
			}
			else
			{
				payoutFraction = 0.05f;
			}

			int rewardPerPackage = __instance.totalPrice / __instance.goodCount;
			__result = Mathf.RoundToInt(rewardPerPackage * payoutFraction);
			return false;
		}
	}
}
