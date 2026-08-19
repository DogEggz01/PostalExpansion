using UnityEngine;

namespace PostalExpansion
{
	internal static class PostalMail
	{
		internal const int RegularMailItemIndex = 221;
		internal const int RegisteredLetterItemIndex = 238;
		internal const int ExpressMailItemIndex = 239;

		internal static bool IsMailPrefab(SaveablePrefab saveable)
		{
			return saveable != null &&
				(saveable.prefabIndex == RegularMailItemIndex ||
				IsRegisteredLetter(saveable) ||
				IsExpressMail(saveable));
		}

		internal static bool IsRegisteredLetter(SaveablePrefab saveable)
		{
			return IsOwnedPrefab(
				saveable,
				RegisteredLetterItemIndex,
				Letter.Prefab);
		}

		internal static bool IsExpressMail(SaveablePrefab saveable)
		{
			return IsOwnedPrefab(
				saveable,
				ExpressMailItemIndex,
				PostalExpressMail.Prefab);
		}

		internal static bool IsWithinVanillaReputationRange(Port origin, float distance)
		{
			return origin != null &&
				distance <= PlayerReputation.GetMaxDistance(origin.region);
		}

		private static bool IsOwnedPrefab(
			SaveablePrefab saveable,
			int prefabIndex,
			GameObject expectedPrefab)
		{
			return saveable != null &&
				saveable.prefabIndex == prefabIndex &&
				PostalPrefabRegistration.IsRegistered(
					PrefabsDirectory.instance,
					prefabIndex,
					expectedPrefab);
		}
	}
}
