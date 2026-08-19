using UnityEngine;

namespace PostalExpansion
{
	internal sealed class FeyValleyWaterfallManorVendorMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly FeyValleyWaterfallManorVendorMission Instance = new();

		private FeyValleyWaterfallManorVendorMission()
			: base(
				"fey_valley_waterfall_manor_vendor",
				"Fey Valley",
				SailwindPortIndex.FeyValley,
				new Vector3(-126.239f, 51.409f, 42.779f),
				new Vector3(0f, 300.007f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.NewPort,
					SailwindPortIndex.AestraAbbey
				},
				"Waterfall manor vendor")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.AestraAbbey:
					return "Gods be praised! This year should be a good harvest year!";

				case SailwindPortIndex.HappyBay:
				case SailwindPortIndex.NewPort:
					return "We're supposed to bring their fruit back, not the other way around!";

				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.DragonCliffs:
					return "I will let the Abbot know about the orders.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
