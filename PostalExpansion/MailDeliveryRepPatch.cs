using HarmonyLib;
using UnityEngine;

namespace PostalExpansion
{
	[HarmonyPatch(typeof(Mission), "GetDeliveryRep")]
	internal static class MailDeliveryRepPatch
	{
		private const int SeverelyLateExpressMissionPenalty = -100;
		private const int SeverelyLateUrgentMissionPenalty = -200;

		private static bool Prefix(Mission __instance, ref int __result)
		{
			if (__instance == null || __instance.goodPrefab == null)
			{
				return true;
			}

			SaveablePrefab saveable = __instance.goodPrefab.GetComponent<SaveablePrefab>();
			if (saveable == null)
			{
				return true;
			}

			if (PostalMail.IsRegisteredLetter(saveable))
			{
				int letterDaysLate =
					Mathf.Max(0, GameState.day - __instance.dueDay);
				if (letterDaysLate >= 2)
				{
					__result =
						LetterMissions.GetSeverelyLatePenalty(__instance);
				}
				else if (letterDaysLate == 1)
				{
					__result = 0;
				}
				else
				{
					__result = Mathf.RoundToInt(
						__instance.distance /
						__instance.goodCount *
						Plugin.RegularMailReputationMultiplier.Value *
						LetterMissions.GetReputationMultiplier(__instance));
				}

				return false;
			}

			bool expressMail = PostalMail.IsExpressMail(saveable);
			bool urgentMail = expressMail && UrgentExpressMail.IsUrgent(__instance);
			float rewardMultiplier = Plugin.RegularMailReputationMultiplier.Value;
			if (expressMail)
			{
				rewardMultiplier *= PostalExpressMail.GetExpressRewardMultiplier(__instance.destinationPort);
				if (urgentMail)
				{
					rewardMultiplier *= UrgentExpressMail.RewardMultiplier;
				}
			}
			else if (saveable.prefabIndex != PostalMail.RegularMailItemIndex)
			{
				return true;
			}

			int daysLate = Mathf.Max(0, GameState.day - __instance.dueDay);
			float baseReputation =
				__instance.distance / __instance.goodCount * rewardMultiplier;

			if (!expressMail)
			{
				float latePenalty = Mathf.Min(daysLate * 0.2f, 1f);
				__result = Mathf.RoundToInt(
					baseReputation - baseReputation * latePenalty);
				return false;
			}

			if (urgentMail)
			{
				if (daysLate >= 2)
				{
					__result = SeverelyLateUrgentMissionPenalty;
				}
				else if (daysLate == 1)
				{
					__result = 0;
				}
				else
				{
					__result = Mathf.RoundToInt(baseReputation);
				}

				return false;
			}

			if (daysLate >= 3)
			{
				__result = SeverelyLateExpressMissionPenalty;
				return false;
			}

			float expressLatePenalty = Mathf.Min(daysLate * 0.5f, 1f);
			__result = Mathf.RoundToInt(
				baseReputation - baseReputation * expressLatePenalty);
			return false;
		}
	}
}
