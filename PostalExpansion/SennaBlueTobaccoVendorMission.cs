using UnityEngine;

namespace PostalExpansion
{
	internal sealed class SennaBlueTobaccoVendorMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly SennaBlueTobaccoVendorMission Instance = new();

		private SennaBlueTobaccoVendorMission()
			: base(
				"senna_blue_tobacco_vendor",
				"Sen'na",
				SailwindPortIndex.Senna,
				new Vector3(85.640f, -0.105f, -173.198f),
				new Vector3(0f, 215.361f, 0f),
				new Vector3(3f, 3f, 3f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.Chronos,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.NewPort,
					SailwindPortIndex.Oasis,
					SailwindPortIndex.SerpentIsle
				},
				"Blue Tobacco vendor")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.DragonCliffs:
					return "Those tobacco from sage hills is obviously inferior.";

				case SailwindPortIndex.Chronos:
					return "Hmm...this might need special preservation.";

				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.HappyBay:
				case SailwindPortIndex.NewPort:
				case SailwindPortIndex.Oasis:
				case SailwindPortIndex.SerpentIsle:
					return "Blue Dream worth every dragons you spend on it.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
