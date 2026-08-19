using UnityEngine;

namespace PostalExpansion
{
	internal sealed class DragonCliffsChimesHutMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly DragonCliffsChimesHutMission Instance = new();

		private DragonCliffsChimesHutMission()
			: base(
				"dragon_cliffs_small_hut_beside_chimes",
				"Dragon Cliffs",
				SailwindPortIndex.DragonCliffs,
				new Vector3(-45.037f, 0.070f, -119.983f),
				new Vector3(0f, 148.902f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.SageHills,
					SailwindPortIndex.TurtleIsland,
					SailwindPortIndex.Onna,
					SailwindPortIndex.Sunspire,
					SailwindPortIndex.AlNilem,
					SailwindPortIndex.Senna,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.FireflyGrotto,
					SailwindPortIndex.Sanctuary,
					SailwindPortIndex.Chronos
				},
				"Small hut beside chimes")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.SageHills:
				case SailwindPortIndex.TurtleIsland:
				case SailwindPortIndex.Onna:
				case SailwindPortIndex.Sunspire:
				case SailwindPortIndex.AlNilem:
				case SailwindPortIndex.Senna:
				case SailwindPortIndex.KiciaBay:
				case SailwindPortIndex.FireflyGrotto:
				case SailwindPortIndex.Sanctuary:
					return "(Gasp) It finally arrived! Thanks!";

				case SailwindPortIndex.Chronos:
					return "What took you so long?";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
