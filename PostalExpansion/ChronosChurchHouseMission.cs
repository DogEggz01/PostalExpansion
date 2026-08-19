using UnityEngine;

namespace PostalExpansion
{
	internal sealed class ChronosChurchHouseMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly ChronosChurchHouseMission Instance = new();

		private ChronosChurchHouseMission()
			: base(
				"chronos_church_house",
				"Chronos",
				SailwindPortIndex.Chronos,
				new Vector3(-19.175f, 31.109f, -62.909f),
				new Vector3(0f, 17.783f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.AestraAbbey,
					SailwindPortIndex.FireflyGrotto,
					SailwindPortIndex.AlAnkhAcademy,
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.DragonCliffs
				},
				"Church house")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.AestraAbbey:
				case SailwindPortIndex.FireflyGrotto:
					return "There is little common ground after the schism.";

				case SailwindPortIndex.AlAnkhAcademy:
					return "We appreciate the knowledge seeking.";

				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.DragonCliffs:
					return "The Church is under maintenance thus not open for visiting.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
