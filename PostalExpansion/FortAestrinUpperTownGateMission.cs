using UnityEngine;

namespace PostalExpansion
{
	internal sealed class FortAestrinUpperTownGateMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly FortAestrinUpperTownGateMission Instance = new();

		private FortAestrinUpperTownGateMission()
			: base(
				"fort_aestrin_upper_town_gate",
				"Fort Aestrin",
				SailwindPortIndex.FortAestrin,
				new Vector3(112.355f, 35.876f, -61.713f),
				new Vector3(359.526f, 116.915f, 359.692f),
				new Vector3(3f, 3f, 3f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.Oasis,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.NewPort,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.AestraAbbey,
					SailwindPortIndex.FireflyGrotto,
					SailwindPortIndex.Eastwind,
					SailwindPortIndex.Chronos
				},
				"Upper Town Gate")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.Oasis:
				case SailwindPortIndex.KiciaBay:
				case SailwindPortIndex.DragonCliffs:
				case SailwindPortIndex.Eastwind:
					return "Your job here is done, courier. Now move along.";

				case SailwindPortIndex.NewPort:
				case SailwindPortIndex.HappyBay:
					return "I wonder when we'll get the next shipment of that colorful tobacco.";

				case SailwindPortIndex.AestraAbbey:
				case SailwindPortIndex.FireflyGrotto:
					return "God be praised!";

				case SailwindPortIndex.Chronos:
					return "You've come a long way, my friend!";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
