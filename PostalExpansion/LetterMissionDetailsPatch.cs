using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PostalExpansion
{
	[HarmonyPatch(typeof(MissionDetailsUI), "InsertMissionData")]
	internal static class LetterMissionDetailsPatch
	{
		private const float LocationLabelPosition = 0.45f;
		private const float LocationValuePosition = 0.80f;
		private const float DeliveryHoursValueOffset = 0.50f;

		private const string LocationLabelObjectName =
			"postal_expansion_letter_location_label";
		private const string LocationValueObjectName =
			"postal_expansion_letter_location_value";
		private const string DeliveryHoursLabelObjectName =
			"postal_expansion_letter_delivery_hours_label";
		private const string DeliveryHoursValueObjectName =
			"postal_expansion_letter_delivery_hours_value";

		private static readonly FieldInfo LocationTextField =
			AccessTools.Field(typeof(MissionDetailsUI), "locationText");
		private static readonly FieldInfo DestinationTextField =
			AccessTools.Field(typeof(MissionDetailsUI), "destination");
		private static readonly FieldInfo DueTextField =
			AccessTools.Field(typeof(MissionDetailsUI), "due");
		private static readonly FieldInfo DistanceTextField =
			AccessTools.Field(typeof(MissionDetailsUI), "distance");
		private static readonly FieldInfo TotalGoldTextField =
			AccessTools.Field(typeof(MissionDetailsUI), "totalGold");
		private static readonly FieldInfo MapRendererField =
			AccessTools.Field(typeof(MissionDetailsUI), "mapRenderer");

		private static void Postfix(MissionDetailsUI __instance, Mission mission)
		{
			if (__instance == null ||
				LocationTextField == null ||
				DestinationTextField == null ||
				DueTextField == null ||
				DistanceTextField == null ||
				TotalGoldTextField == null)
			{
				return;
			}

			var destinationValue = DestinationTextField.GetValue(__instance) as TextMesh;
			var dueValue = DueTextField.GetValue(__instance) as TextMesh;
			var distanceText = DistanceTextField.GetValue(__instance) as TextMesh;
			var totalGoldText = TotalGoldTextField.GetValue(__instance) as TextMesh;
			if (destinationValue == null ||
				dueValue == null ||
				distanceText == null ||
				totalGoldText == null)
			{
				return;
			}

			Transform textRoot = destinationValue.transform.parent;
			Transform deliveryTextRoot = distanceText.transform.parent;
			TextMesh locationLabel = FindText(textRoot, LocationLabelObjectName);
			TextMesh locationValue = FindText(textRoot, LocationValueObjectName);
			TextMesh deliveryHoursLabel = FindText(
				deliveryTextRoot,
				DeliveryHoursLabelObjectName);
			TextMesh deliveryHoursValue = FindText(
				deliveryTextRoot,
				DeliveryHoursValueObjectName);
			TextMesh destinationLabel = FindTextByContent(
				textRoot,
				"Destination:");
			TextMesh cargoLabel = FindTextStartingWith(textRoot, "Cargo:");
			if (!LetterMissions.TryGetDefinition(
					mission,
					out LetterMissionDefinition definition))
			{
				SetActive(destinationLabel, true);
				SetActive(destinationValue, true);
				SetActive(locationLabel, false);
				SetActive(locationValue, false);
				SetActive(deliveryHoursLabel, false);
				SetActive(deliveryHoursValue, false);
				return;
			}

			totalGoldText.text =
				LetterMissions.GetFixedGoldReward(mission) + " " +
				PlayerGold.GetCurrencyName((int)Currency.gold);

			if (destinationLabel == null || cargoLabel == null)
			{
				Debug.LogWarning(
					"Postal Expansion: mission detail labels were not found.");
				return;
			}

			locationLabel = locationLabel ?? CloneText(
				destinationLabel,
				LocationLabelObjectName);
			locationValue = locationValue ?? CloneText(
				destinationValue,
				LocationValueObjectName);
			if (locationLabel == null || locationValue == null)
			{
				return;
			}

			if (definition.HideDestination)
			{
				locationLabel.transform.localPosition =
					destinationLabel.transform.localPosition;
				locationValue.transform.localPosition =
					destinationValue.transform.localPosition;
			}
			else
			{
				float destinationY = destinationValue.transform.localPosition.y;
				float cargoY = cargoLabel.transform.localPosition.y;
				SetLocalY(
					locationLabel.transform,
					Mathf.Lerp(destinationY, cargoY, LocationLabelPosition));
				SetLocalY(
					locationValue.transform,
					Mathf.Lerp(destinationY, cargoY, LocationValuePosition));
			}
			locationLabel.text = "Location:";
			locationValue.text = definition.LocationDescription;
			SetActive(destinationLabel, !definition.HideDestination);
			SetActive(destinationValue, !definition.HideDestination);
			SetActive(locationLabel, true);
			SetActive(locationValue, true);

			deliveryHoursLabel = deliveryHoursLabel ?? CloneText(
				distanceText,
				DeliveryHoursLabelObjectName);
			deliveryHoursValue = deliveryHoursValue ?? CloneText(
				dueValue,
				DeliveryHoursValueObjectName);
			if (deliveryHoursLabel != null && deliveryHoursValue != null)
			{
				float deliveryHoursY = Mathf.Lerp(
					dueValue.transform.localPosition.y,
					distanceText.transform.localPosition.y,
					0.5f);
				SetLocalY(deliveryHoursLabel.transform, deliveryHoursY);
				SetLocalY(deliveryHoursValue.transform, deliveryHoursY);
				deliveryHoursLabel.text = "Delivery hours:";
				deliveryHoursValue.text =
					LetterDeliveryHours.GetDisplayHours(definition.DeliveryWindow);
				SetActive(deliveryHoursLabel, true);
				SetActive(deliveryHoursValue, true);
				PositionDeliveryHoursValue(
					deliveryHoursLabel,
					deliveryHoursValue);
			}

			var mapLocationText = LocationTextField.GetValue(__instance) as TextMesh;
			var mapRenderer = MapRendererField?.GetValue(__instance) as Renderer;
			if (definition.ShowDeliveryCoordinates)
			{
				if (mapRenderer != null)
				{
					mapRenderer.gameObject.SetActive(false);
				}

				if (mapLocationText != null)
				{
					mapLocationText.text =
						FloatingOriginManager.instance != null &&
						LetterDeliveryAreaCreationPatch.TryGetAreaTransform(
							definition,
							out Transform deliveryArea)
							? GetVanillaLocationText(deliveryArea)
							: "approximate location:\nunavailable";
				}
			}
			else
			{
				if (mapLocationText != null)
				{
					mapLocationText.text = string.Empty;
				}
			}
		}

		private static string GetVanillaLocationText(Transform deliveryArea)
		{
			Vector3 globeCoordinates =
				FloatingOriginManager.instance.GetGlobeCoords(deliveryArea);
			int longitude = Mathf.RoundToInt(globeCoordinates.x);
			int latitude = Mathf.RoundToInt(globeCoordinates.z);
			string eastWest = longitude < 0 ? "W" : "E";
			string northSouth = latitude < 0 ? "S" : "N";

			return "approximate location:\n" +
				latitude + " " + northSouth + ", " +
				longitude + " " + eastWest;
		}

		private static TextMesh CloneText(TextMesh source, string objectName)
		{
			if (source == null || source.transform.parent == null)
			{
				return null;
			}

			GameObject clone = Object.Instantiate(
				source.gameObject,
				source.transform.parent);
			clone.name = objectName;
			return clone.GetComponent<TextMesh>();
		}

		private static TextMesh FindText(Transform root, string objectName)
		{
			Transform child = root != null ? root.Find(objectName) : null;
			return child != null ? child.GetComponent<TextMesh>() : null;
		}

		private static TextMesh FindTextByContent(Transform root, string content)
		{
			return FindText(root, text => string.Equals(
				text.Trim(),
				content,
				StringComparison.Ordinal));
		}

		private static TextMesh FindTextStartingWith(Transform root, string content)
		{
			return FindText(root, text => text.TrimStart().StartsWith(
				content,
				StringComparison.Ordinal));
		}

		private static TextMesh FindText(
			Transform root,
			Func<string, bool> predicate)
		{
			if (root == null)
			{
				return null;
			}

			foreach (TextMesh candidate in root.GetComponentsInChildren<TextMesh>(true))
			{
				if (candidate != null && predicate(candidate.text ?? string.Empty))
				{
					return candidate;
				}
			}

			return null;
		}

		private static void SetLocalY(Transform target, float y)
		{
			Vector3 position = target.localPosition;
			position.y = y;
			target.localPosition = position;
		}

		private static void PositionDeliveryHoursValue(
			TextMesh label,
			TextMesh value)
		{
			Vector3 textDirection =
				label.transform.localRotation * Vector3.right;
			Vector3 valueLocalPosition =
				label.transform.localPosition +
				textDirection.normalized * DeliveryHoursValueOffset;
			valueLocalPosition.y = value.transform.localPosition.y;
			valueLocalPosition.z = value.transform.localPosition.z;
			value.transform.localPosition = valueLocalPosition;
		}

		private static void SetActive(TextMesh text, bool active)
		{
			if (text != null)
			{
				text.gameObject.SetActive(active);
			}
		}
	}
}
