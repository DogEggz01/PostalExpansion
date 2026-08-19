using UnityEngine;

namespace PostalExpansion
{
	internal sealed class CrabBeachHouseMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly CrabBeachHouseMission Instance = new();

		private CrabBeachHouseMission()
			: base(
				"crab_beach_house",
				"Crab Beach",
				SailwindPortIndex.CrabBeach,
				new Vector3(-100.447f, 4.350f, 30.655f),
				new Vector3(0f, 188.793f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.Chronos
				},
				"House at beach")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.DragonCliffs:
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.KiciaBay:
					return "I SAID I DON'T BUILD SHIPS ANYMORE! GO AWAY!";

				case SailwindPortIndex.Chronos:
					return "...Where?";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
