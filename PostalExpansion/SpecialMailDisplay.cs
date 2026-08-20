using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace PostalExpansion
{
	internal enum SpecialMailKind
	{
		None = 0,
		AnonymousLetter = 1,
		UrgentExpressMail = 2,
		GoldenDelivery = 3
	}

	internal static class SpecialMailDisplay
	{
		internal static SpecialMailKind CurrentDeliveryKind { get; private set; }

		internal static SpecialMailKind GetKind(Mission mission)
		{
			if (GoldenDeliveryMissions.IsGolden(mission))
			{
				return SpecialMailKind.GoldenDelivery;
			}
			if (UrgentExpressMail.IsUrgent(mission))
			{
				return SpecialMailKind.UrgentExpressMail;
			}
			if (AnonymousLetterMissions.TryGetDefinition(mission, out _))
			{
				return SpecialMailKind.AnonymousLetter;
			}

			return SpecialMailKind.None;
		}

		internal static string GetDisplayName(SpecialMailKind kind)
		{
			switch (kind)
			{
				case SpecialMailKind.AnonymousLetter:
					return "Anonymous Letter";
				case SpecialMailKind.UrgentExpressMail:
					return "Urgent Express Mail";
				case SpecialMailKind.GoldenDelivery:
					return GoldenDeliveryMissions.DisplayName;
				default:
					return string.Empty;
			}
		}

		internal static void BeginDelivery(Mission mission)
		{
			CurrentDeliveryKind = GetKind(mission);
		}

		internal static void EndDelivery()
		{
			CurrentDeliveryKind = SpecialMailKind.None;
		}
	}

	[HarmonyPatch(typeof(Mission), nameof(Mission.DeliverGood))]
	internal static class SpecialMailDeliveryContextPatch
	{
		private static void Prefix(Mission __instance)
		{
			SpecialMailDisplay.BeginDelivery(__instance);
		}

		private static Exception Finalizer(Exception __exception)
		{
			SpecialMailDisplay.EndDelivery();
			return __exception;
		}
	}

	[HarmonyPatch(
		typeof(NotificationUi),
		nameof(NotificationUi.ShowNotification),
		new[] { typeof(string) })]
	internal static class SpecialMailDeliveryNotificationPatch
	{
		private static void Prefix(ref string notification)
		{
			SpecialMailKind kind = SpecialMailDisplay.CurrentDeliveryKind;
			if (kind == SpecialMailKind.None ||
				string.IsNullOrEmpty(notification) ||
				!notification.StartsWith("Delivered ", StringComparison.Ordinal))
			{
				return;
			}

			int lineBreak = notification.IndexOf('\n');
			string suffix = lineBreak >= 0
				? notification.Substring(lineBreak)
				: string.Empty;
			notification =
				"Delivered " + SpecialMailDisplay.GetDisplayName(kind) + suffix;
		}
	}

	[HarmonyPatch(typeof(MissionDetailsUI), "InsertMissionData")]
	internal static class SpecialMailMissionDetailsPatch
	{
		private static readonly FieldInfo CargoNameField =
			AccessTools.Field(typeof(MissionDetailsUI), "cargoName");

		private static void Postfix(MissionDetailsUI __instance, Mission mission)
		{
			SpecialMailKind kind = SpecialMailDisplay.GetKind(mission);
			if (kind == SpecialMailKind.None)
			{
				return;
			}

			var cargoName = CargoNameField?.GetValue(__instance) as TextMesh;
			if (cargoName != null)
			{
				cargoName.text = SpecialMailDisplay.GetDisplayName(kind);
			}
		}
	}

	[HarmonyPatch(typeof(ShipItem), nameof(ShipItem.UpdateLookText))]
	internal static class SpecialMailItemLookTextPatch
	{
		private static void Postfix(ShipItem __instance)
		{
			Good good = __instance?.GetComponent<Good>();
			int missionIndex = good != null ? good.GetMissionIndex() : -1;
			if (missionIndex < 0 ||
				PlayerMissions.missions == null ||
				missionIndex >= PlayerMissions.missions.Length)
			{
				return;
			}

			Mission mission = PlayerMissions.missions[missionIndex];
			SpecialMailKind kind = SpecialMailDisplay.GetKind(mission);
			if (kind != SpecialMailKind.GoldenDelivery &&
				kind != SpecialMailKind.UrgentExpressMail)
			{
				return;
			}

			__instance.lookText =
				SpecialMailDisplay.GetDisplayName(kind) +
				"\nto " + mission.destinationPort.GetPortName() +
				"\ndue: " + mission.GetDueText();
		}
	}
}
