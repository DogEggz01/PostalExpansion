using UnityEngine;

namespace PostalExpansion
{
	internal sealed class SanctuaryMainBuildingMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly SanctuaryMainBuildingMission Instance = new();

		private SanctuaryMainBuildingMission()
			: base(
				"sanctuary_main_building",
				"Sanctuary",
				SailwindPortIndex.Sanctuary,
				new Vector3(28.065f, 3.492f, -28.618f),
				new Vector3(0f, 88.481f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.AlchemistsIsland,
					SailwindPortIndex.AlAnkhAcademy,
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.AestraAbbey,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.TurtleIsland,
					SailwindPortIndex.SageHills,
					SailwindPortIndex.Chronos
				},
				"Main Building")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.AlchemistsIsland:
				case SailwindPortIndex.AlAnkhAcademy:
					return "(You hear one bell ring.)";

				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.AestraAbbey:
				case SailwindPortIndex.HappyBay:
					return "(You hear two bells ring.)";

				case SailwindPortIndex.DragonCliffs:
				case SailwindPortIndex.TurtleIsland:
				case SailwindPortIndex.SageHills:
					return "(You hear three bells ring.)";

				case SailwindPortIndex.Chronos:
					return "...Namo Amituofo.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
