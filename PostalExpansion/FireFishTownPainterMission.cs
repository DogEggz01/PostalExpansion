using UnityEngine;

namespace PostalExpansion
{
	internal sealed class FireFishTownPainterMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly FireFishTownPainterMission Instance = new();

		private FireFishTownPainterMission()
			: base(
				"fire_fish_town_painter",
				"Fire Fish Town",
				SailwindPortIndex.FireFishTown,
				new Vector3(7.186f, 3.075f, 216.135f),
				new Vector3(0f, 190.815f, 0f),
				new Vector3(3f, 3f, 3f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.Chronos,
					SailwindPortIndex.HappyBay
				},
				"Painter")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.GoldRockCity:
					return "Always happy to hear from fellow painters!";

				case SailwindPortIndex.DragonCliffs:
					return "Ugh! I escape there for a reason!";

				case SailwindPortIndex.HappyBay:
					return "Daddy...";

				case SailwindPortIndex.Chronos:
					return "I would like to visit there one day!";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
