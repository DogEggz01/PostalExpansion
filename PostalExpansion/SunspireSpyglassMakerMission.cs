using UnityEngine;

namespace PostalExpansion
{
	internal sealed class SunspireSpyglassMakerMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly SunspireSpyglassMakerMission Instance = new();

		private SunspireSpyglassMakerMission()
			: base(
				"sunspire_spyglass_maker",
				"Sunspire",
				SailwindPortIndex.Sunspire,
				new Vector3(37.768f, 6.183f, 133.613f),
				new Vector3(0f, 67.185f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.AlAnkhAcademy,
					SailwindPortIndex.AlchemistsIsland,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.AestraAbbey,
					SailwindPortIndex.FireflyGrotto,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.NewPort,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.Chronos,
					SailwindPortIndex.GoldRockCity
				},
				"Spyglass Maker")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.AlAnkhAcademy:
					return "I'll make this my priority. Thank you for the letter.";

				case SailwindPortIndex.AlchemistsIsland:
				case SailwindPortIndex.DragonCliffs:
				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.FireflyGrotto:
				case SailwindPortIndex.HappyBay:
				case SailwindPortIndex.NewPort:
				case SailwindPortIndex.KiciaBay:
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.Chronos:
					return "I already have a two-month queue! Oh God...";

				case SailwindPortIndex.AestraAbbey:
					return "I wonder where they get all these coins...";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
