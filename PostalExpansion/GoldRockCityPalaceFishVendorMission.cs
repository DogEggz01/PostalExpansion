using UnityEngine;

namespace PostalExpansion
{
	internal sealed class GoldRockCityPalaceFishVendorMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly GoldRockCityPalaceFishVendorMission Instance = new();

		private GoldRockCityPalaceFishVendorMission()
			: base(
				"gold_rock_city_palace_fish_vendor",
				"Gold Rock City",
				SailwindPortIndex.GoldRockCity,
				new Vector3(-130.644f, 2.269f, 70.875f),
				new Vector3(0f, 226.858f, 0f),
				new Vector3(2f, 2f, 2f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.AlbacoreTown,
					SailwindPortIndex.FireFishTown,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.Oasis
				},
				"Fish vendor at palace")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.AlbacoreTown:
				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.KiciaBay:
				case SailwindPortIndex.FireFishTown:
					return "People love exotic fish!";

				case SailwindPortIndex.DragonCliffs:
				case SailwindPortIndex.HappyBay:
					return "Eels... not sure about that.";

				case SailwindPortIndex.Oasis:
					return VanillaQuestState.IsHideoutQuestComplete()
						? "Our mutual friend has come up with some new stuff, eh?"
						: "I will need a fast boat for this delivery.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
