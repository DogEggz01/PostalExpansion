using UnityEngine;

namespace PostalExpansion
{
	internal sealed class DragonCliffsBrownTobaccoVendorMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly DragonCliffsBrownTobaccoVendorMission Instance = new();

		private DragonCliffsBrownTobaccoVendorMission()
			: base(
				"dragon_cliffs_brown_tobacco_vendor",
				"Dragon Cliffs",
				SailwindPortIndex.DragonCliffs,
				new Vector3(47.025f, 1.993f, -11.703f),
				new Vector3(0f, 106.371f, 0f),
				new Vector3(3f, 3f, 3f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.NewPort,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.FireflyGrotto
				},
				"Brown Tobacco Vendor")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.NewPort:
				case SailwindPortIndex.HappyBay:
				case SailwindPortIndex.FireflyGrotto:
					return "(Sigh) How much longer do I need to stay here...?";

				case SailwindPortIndex.GoldRockCity:
					return "Hmm...";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
