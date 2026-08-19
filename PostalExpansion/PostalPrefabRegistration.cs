using System;
using UnityEngine;

namespace PostalExpansion
{
	internal static class PostalPrefabRegistration
	{
		internal static bool IsRegistered(PrefabsDirectory directory, int prefabIndex, GameObject prefab)
		{
			return directory != null &&
				directory.directory != null &&
				directory.shipItems != null &&
				prefab != null &&
				directory.directory.Length > prefabIndex &&
				directory.shipItems.Length > prefabIndex &&
				directory.directory[prefabIndex] == prefab &&
				directory.shipItems[prefabIndex] == prefab.GetComponent<ShipItem>();
		}

		internal static bool IsSlotOccupiedByAnotherPrefab(
			PrefabsDirectory directory,
			int prefabIndex,
			GameObject expectedPrefab)
		{
			if (directory == null)
			{
				return false;
			}

			if (directory.directory != null &&
				directory.directory.Length > prefabIndex &&
				directory.directory[prefabIndex] != null &&
				directory.directory[prefabIndex] != expectedPrefab)
			{
				return true;
			}

			ShipItem expectedShipItem = expectedPrefab != null
				? expectedPrefab.GetComponent<ShipItem>()
				: null;
			return directory.shipItems != null &&
				directory.shipItems.Length > prefabIndex &&
				directory.shipItems[prefabIndex] != null &&
				directory.shipItems[prefabIndex] != expectedShipItem;
		}

		internal static bool EnsureCapacity(PrefabsDirectory directory, int prefabIndex)
		{
			if (directory == null || directory.directory == null)
			{
				return false;
			}

			int requiredSize = prefabIndex + 1;
			if (directory.directory.Length < requiredSize)
			{
				Array.Resize(ref directory.directory, requiredSize);
			}

			if (directory.shipItems == null)
			{
				directory.shipItems = new ShipItem[directory.directory.Length];
			}
			else if (directory.shipItems.Length < requiredSize)
			{
				Array.Resize(ref directory.shipItems, requiredSize);
			}

			return true;
		}

		internal static bool RegisterShipItem(PrefabsDirectory directory, int prefabIndex, GameObject prefab)
		{
			if (directory == null ||
				directory.shipItems == null ||
				directory.shipItems.Length <= prefabIndex ||
				prefab == null)
			{
				return false;
			}

			ShipItem shipItem = prefab.GetComponent<ShipItem>();
			if (shipItem == null)
			{
				return false;
			}

			directory.shipItems[prefabIndex] = shipItem;
			return true;
		}
	}
}
