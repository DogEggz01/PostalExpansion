using HarmonyLib;
using UnityEngine;

namespace PostalExpansion
{
	[HarmonyPatch(typeof(PortDude), "Awake")]
	internal static class LetterDeliveryAreaCreationPatch
	{
		private const string AreaRootPrefix =
			"postal_expansion_letter_";

		private static void Postfix(PortDude __instance)
		{
			if (__instance == null || __instance.GetPort() == null)
			{
				return;
			}

			foreach (LetterMissionDefinition definition in
				LetterMissionDefinitions.All)
			{
				if (definition.DestinationPortIndex ==
					__instance.GetPort().portIndex)
				{
					EnsureArea(__instance, definition);
				}
			}
		}

		private static void EnsureArea(
			PortDude portDude,
			LetterMissionDefinition definition)
		{
			string rootName = AreaRootPrefix + definition.Id;
			Transform parent = GetAreaParent(portDude, definition);
			if (parent == null || parent.Find(rootName) != null)
			{
				return;
			}

			var root = new GameObject(rootName);
			if (definition.UsePersistentWorldAnchor)
			{
				root.transform.position =
					portDude.transform.TransformPoint(definition.LocalPosition);
				root.transform.rotation =
					portDude.transform.rotation *
					Quaternion.Euler(definition.LocalEulerAngles);
				root.transform.SetParent(parent, true);
			}
			else
			{
				root.transform.SetParent(parent, false);
				root.transform.localPosition = definition.LocalPosition;
				root.transform.localRotation =
					Quaternion.Euler(definition.LocalEulerAngles);
			}

			LetterDialoguePresenceArea dialogueArea =
				CreateDialogueArea(root, definition);
			CreateDeliveryArea(
				root,
				portDude.GetPort(),
				definition,
				dialogueArea);

			if (definition.UsePersistentWorldAnchor)
			{
				PersistentLetterDeliveryAnchor anchor =
					root.AddComponent<PersistentLetterDeliveryAnchor>();
				anchor.Initialize(
					portDude.transform,
					definition.LocalPosition,
					definition.LocalEulerAngles);
			}
		}

		private static Transform GetAreaParent(
			PortDude portDude,
			LetterMissionDefinition definition)
		{
			if (!definition.UsePersistentWorldAnchor)
			{
				return portDude.transform;
			}

			if (Refs.shiftingWorld != null)
			{
				return Refs.shiftingWorld;
			}

			FloatingOriginManager manager =
				Object.FindObjectOfType<FloatingOriginManager>();
			if (manager != null)
			{
				return manager.transform;
			}

			Debug.LogWarning(
				"Postal Expansion: Cannot create persistent delivery area " +
				definition.Id + "; shifting world is unavailable.");
			return null;
		}

		internal static bool TryGetAreaTransform(
			LetterMissionDefinition definition,
			out Transform areaTransform)
		{
			areaTransform = null;
			if (definition == null)
			{
				return false;
			}

			Transform parent = null;
			if (definition.UsePersistentWorldAnchor)
			{
				parent = Refs.shiftingWorld;
			}
			else if (Port.ports != null &&
				definition.DestinationPortIndex >= 0 &&
				definition.DestinationPortIndex < Port.ports.Length)
			{
				Port destination = Port.ports[definition.DestinationPortIndex];
				PortDude portDude = destination != null
					? destination.GetDude()
					: null;
				parent = portDude != null ? portDude.transform : null;
			}

			areaTransform = parent != null
				? parent.Find(AreaRootPrefix + definition.Id)
				: null;
			return areaTransform != null;
		}

		private static LetterDialoguePresenceArea CreateDialogueArea(
			GameObject root,
			LetterMissionDefinition definition)
		{
			var areaObject = new GameObject(
				"postal_expansion_letter_dialogue_" + definition.Id);
			areaObject.layer = 2;
			areaObject.transform.SetParent(root.transform, false);

			BoxCollider collider = areaObject.AddComponent<BoxCollider>();
			collider.isTrigger = true;
			collider.size = definition.TriggerSize * 5f;
			AddTriggerRigidbody(areaObject);

			LetterDialoguePresenceArea area =
				areaObject.AddComponent<LetterDialoguePresenceArea>();
			area.Initialize(definition);
			return area;
		}

		private static void CreateDeliveryArea(
			GameObject root,
			Port port,
			LetterMissionDefinition definition,
			LetterDialoguePresenceArea dialogueArea)
		{
			var areaObject = new GameObject(
				"postal_expansion_letter_delivery_" + definition.Id);
			areaObject.layer = 2;
			areaObject.transform.SetParent(root.transform, false);

			BoxCollider collider = areaObject.AddComponent<BoxCollider>();
			collider.isTrigger = true;
			collider.size = definition.TriggerSize;
			AddTriggerRigidbody(areaObject);

			LetterDeliveryArea area = areaObject.AddComponent<LetterDeliveryArea>();
			area.Initialize(port, definition, dialogueArea);
		}

		private static void AddTriggerRigidbody(GameObject areaObject)
		{
			Rigidbody body = areaObject.AddComponent<Rigidbody>();
			body.isKinematic = true;
			body.useGravity = false;
		}
	}
}
