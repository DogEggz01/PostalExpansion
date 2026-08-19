using HarmonyLib;
using UnityEngine;

namespace PostalExpansion
{
	[HarmonyPatch(typeof(ShipItem), "AddLODGroup")]
	internal static class PostalShipItemLodPatch
	{
		private static bool Prefix(ShipItem __instance)
		{
			SaveablePrefab saveable = __instance?.GetComponent<SaveablePrefab>();
			if (saveable == null ||
				!PostalMail.IsMailPrefab(saveable) ||
				saveable.prefabIndex == PostalMail.RegularMailItemIndex)
			{
				return true;
			}

			LODGroup template = RefsDirectory.instance?.LODtemplateItems;
			LOD[] lods = template?.GetLODs();
			if (lods == null || lods.Length == 0)
			{
				return true;
			}

			LODGroup itemLodGroup = __instance.GetComponent<LODGroup>() ??
				__instance.gameObject.AddComponent<LODGroup>();
			LOD[] configuredLods = (LOD[])lods.Clone();
			configuredLods[0].renderers =
				__instance.GetComponentsInChildren<Renderer>(true);
			itemLodGroup.SetLODs(configuredLods);
			return false;
		}
	}
}
