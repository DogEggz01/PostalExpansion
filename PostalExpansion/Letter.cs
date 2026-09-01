using UnityEngine;
using Object = UnityEngine.Object;

namespace PostalExpansion
{
	internal static class Letter
	{
		internal const int LetterGoodIndex = 68;

		private const int QuestLetterItemIndex = 330;
		private const string LetterName = "Registered Letter";
		private const string InheritedLabelObjectName = "label";
		private const string LetterSealTextureFile = "registered_letter_seal.png";
		private const string LetterSealObjectName = "postal_expansion_registered_letter_seal";
		private const float LetterSealSize = 0.11f;
		private const float LetterSealSurfaceOffset = 0.002f;

		private static GameObject letterPrefab;
		private static readonly PostalSealVisual LetterSeal = new PostalSealVisual(
			LetterSealObjectName,
			LetterSealTextureFile,
			"registered_letter_seal",
			LetterSealSize);

		internal static GameObject Prefab => letterPrefab;

		internal static bool EnsureRegistered(PrefabsDirectory directory)
		{
			if (directory == null || directory.directory == null)
			{
				return false;
			}

			if (PostalPrefabRegistration.IsRegistered(
				directory,
				PostalMail.RegisteredLetterItemIndex,
				letterPrefab))
			{
				return true;
			}

			if (!HasPrefab(directory, PostalMail.RegularMailItemIndex))
			{
				Debug.LogWarning("Postal Expansion: regular mail prefab 221 was not found.");
				return false;
			}

			if (!HasPrefab(directory, QuestLetterItemIndex))
			{
				Debug.LogWarning("Postal Expansion: vanilla quest letter prefab 330 was not found.");
				return false;
			}

			if (PostalPrefabRegistration.IsSlotOccupiedByAnotherPrefab(
				directory,
				PostalMail.RegisteredLetterItemIndex,
				letterPrefab))
			{
				Debug.LogError("Postal Expansion: prefab index 238 is already occupied.");
				return false;
			}

			GameObject regularMailPrefab =
				directory.directory[PostalMail.RegularMailItemIndex];
			GameObject questLetterPrefab = directory.directory[QuestLetterItemIndex];
			if (!HasRequiredComponents(regularMailPrefab, questLetterPrefab) ||
				!PostalPrefabRegistration.EnsureCapacity(
					directory,
					PostalMail.RegisteredLetterItemIndex))
			{
				Debug.LogError(
					"Postal Expansion: Registered Letter prefab components could not be prepared.");
				return false;
			}

			if (letterPrefab == null)
			{
				letterPrefab = Object.Instantiate(regularMailPrefab);
				letterPrefab.name =
					$"{PostalMail.RegisteredLetterItemIndex} ({LetterGoodIndex}) {LetterName}";
				Object.DontDestroyOnLoad(letterPrefab);
			}

			PostalMailOutlineSanitizer.Attach(letterPrefab);
			ConfigurePrefab(letterPrefab, questLetterPrefab);
			if (!PostalPrefabRegistration.RegisterShipItem(
				directory,
				PostalMail.RegisteredLetterItemIndex,
				letterPrefab))
			{
				Debug.LogError(
					"Postal Expansion: Registered Letter prefab could not be registered as a ship item.");
				return false;
			}

			directory.directory[PostalMail.RegisteredLetterItemIndex] = letterPrefab;
			return true;
		}

		private static bool HasPrefab(PrefabsDirectory directory, int prefabIndex)
		{
			return directory.directory.Length > prefabIndex && directory.directory[prefabIndex] != null;
		}

		private static bool HasRequiredComponents(
			GameObject regularMailPrefab,
			GameObject questLetterPrefab)
		{
			return regularMailPrefab.GetComponent<SaveablePrefab>() != null &&
				regularMailPrefab.GetComponent<ShipItem>() != null &&
				regularMailPrefab.GetComponent<Good>() != null &&
				regularMailPrefab.GetComponent<MeshFilter>() != null &&
				regularMailPrefab.GetComponent<MeshRenderer>() != null &&
				regularMailPrefab.GetComponent<BoxCollider>() != null &&
				questLetterPrefab.GetComponent<ShipItem>() != null &&
				questLetterPrefab.GetComponent<MeshFilter>() != null &&
				questLetterPrefab.GetComponent<MeshRenderer>() != null &&
				questLetterPrefab.GetComponent<BoxCollider>() != null;
		}

		private static void ConfigurePrefab(GameObject prefab, GameObject questLetterPrefab)
		{
			prefab.SetActive(true);
			prefab.name =
				$"{PostalMail.RegisteredLetterItemIndex} ({LetterGoodIndex}) {LetterName}";
			prefab.GetComponent<SaveablePrefab>().prefabIndex =
				PostalMail.RegisteredLetterItemIndex;

			ShipItem targetItem = prefab.GetComponent<ShipItem>();
			ShipItem sourceItem = questLetterPrefab.GetComponent<ShipItem>();
			targetItem.name = LetterName;
			targetItem.mass = 1f;
			targetItem.big = sourceItem.big;
			targetItem.holdDistance = sourceItem.holdDistance;
			targetItem.holdHeight = sourceItem.holdHeight;
			targetItem.heldRotationOffset = sourceItem.heldRotationOffset;
			targetItem.inventoryScale = sourceItem.inventoryScale;
			targetItem.inventoryRotation = sourceItem.inventoryRotation;
			targetItem.inventoryRotationX = sourceItem.inventoryRotationX;

			prefab.GetComponent<Good>().requiredRepLevel =
				RegisteredLetterMissions.RequiredReputation;

			MeshFilter sourceFilter = questLetterPrefab.GetComponent<MeshFilter>();
			MeshRenderer sourceRenderer = questLetterPrefab.GetComponent<MeshRenderer>();
			BoxCollider sourceCollider = questLetterPrefab.GetComponent<BoxCollider>();
			prefab.GetComponent<MeshFilter>().sharedMesh = sourceFilter.sharedMesh;
			prefab.GetComponent<MeshRenderer>().sharedMaterials = sourceRenderer.sharedMaterials;

			BoxCollider targetCollider = prefab.GetComponent<BoxCollider>();
			targetCollider.center = sourceCollider.center;
			targetCollider.size = sourceCollider.size;

			Transform inheritedLabel = prefab.transform.Find(InheritedLabelObjectName);
			if (inheritedLabel != null)
			{
				inheritedLabel.gameObject.SetActive(false);
				Object.Destroy(inheritedLabel.gameObject);
			}

			Vector3 sealPosition = targetCollider.center;
			sealPosition.z += targetCollider.size.z / 2f + LetterSealSurfaceOffset;
			LetterSeal.Ensure(prefab, sealPosition, Quaternion.identity);
		}
	}
}
