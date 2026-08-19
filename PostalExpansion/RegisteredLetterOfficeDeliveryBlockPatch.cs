using HarmonyLib;
using UnityEngine;

namespace PostalExpansion
{
	[HarmonyPatch(typeof(PortDude), "OnTriggerEnter")]
	internal static class RegisteredLetterOfficeDeliveryBlockPatch
	{
		private static bool Prefix(Collider other)
		{
			Good good = other != null ? other.GetComponentInParent<Good>() : null;
			SaveablePrefab saveable =
				good != null ? good.GetComponent<SaveablePrefab>() : null;
			return !PostalMail.IsRegisteredLetter(saveable);
		}
	}
}
