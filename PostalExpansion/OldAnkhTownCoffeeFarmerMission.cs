using UnityEngine;

namespace PostalExpansion
{
	internal sealed class OldAnkhTownCoffeeFarmerMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly OldAnkhTownCoffeeFarmerMission Instance = new();

		private OldAnkhTownCoffeeFarmerMission()
			: base(
				"old_ankh_town_coffee_farmer",
				"Old Ankh Town",
				SailwindPortIndex.OldAnkhTown,
				new Vector3(-223.606f, 2.544f, 56.382f),
				new Vector3(0f, 303.025f, 0f),
				new Vector3(2.5f, 2.5f, 2.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.NewPort,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.Eastwind
				},
				"Coffee farmer")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.GoldRockCity:
					return "Glory to Sultan!";

				case SailwindPortIndex.DragonCliffs:
				case SailwindPortIndex.KiciaBay:
					return "I'd love to get some tea in return.";

				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.NewPort:
				case SailwindPortIndex.HappyBay:
				case SailwindPortIndex.Eastwind:
					return "Arabica beans are much better then Emerald's Robusta.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
