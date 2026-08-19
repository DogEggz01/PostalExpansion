using UnityEngine;

namespace PostalExpansion
{
	internal sealed class SageHillsWestTobaccoFarmerMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly SageHillsWestTobaccoFarmerMission Instance = new();

		private SageHillsWestTobaccoFarmerMission()
			: base(
				"sage_hills_west_tobacco_farmer",
				"Sage Hills",
				SailwindPortIndex.SageHills,
				new Vector3(-165.170f, 41.067f, -82.020f),
				new Vector3(0f, 212.161f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.Chronos,
					SailwindPortIndex.SerpentIsle,
					SailwindPortIndex.Oasis,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.NewPort
				},
				"Green tobacco farmer (West Hill)")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.SerpentIsle:
				case SailwindPortIndex.Oasis:
				case SailwindPortIndex.HappyBay:
				case SailwindPortIndex.Chronos:
				case SailwindPortIndex.NewPort:
					return "Best tobacco in the world!";

				case SailwindPortIndex.KiciaBay:
					return "Heh! Tired of the blue one?";

				case SailwindPortIndex.DragonCliffs:
					return "Bi dien ha? This cut is too much!";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
