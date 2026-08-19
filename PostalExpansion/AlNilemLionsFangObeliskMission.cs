using UnityEngine;

namespace PostalExpansion
{
	internal sealed class AlNilemLionsFangObeliskMission
		: AnonymousLetterMissionDefinition
	{
		internal static readonly AlNilemLionsFangObeliskMission Instance = new();

		private AlNilemLionsFangObeliskMission()
			: base(
				"al_nilem_lions_fang_obelisk",
				"Al'Nilem",
				SailwindPortIndex.AlNilem,
				new Vector3(807.097f, 4.672f, 1451.161f),
				new Vector3(0f, 318.985f, 0f),
				new Vector3(5f, 5f, 5f),
				1659.3f,
				new LetterDeliveryWindow(11f, 13f, "11-13"),
				new[]
				{
					SailwindPortIndex.Neverdin,
					SailwindPortIndex.OldAnkhTown,
					SailwindPortIndex.MirageMountain,
					SailwindPortIndex.TurtleIsland,
					SailwindPortIndex.Onna,
					SailwindPortIndex.FireflyGrotto
				},
				"Isle of Clear Mind Obelisk")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.Neverdin:
				case SailwindPortIndex.OldAnkhTown:
				case SailwindPortIndex.MirageMountain:
					return "(Letter burns away after you place it on ground, you feel hotter)";

				case SailwindPortIndex.TurtleIsland:
					return "(Letter open by itself once you approach the obelisk, showing totemic pattern before flying away)";

				case SailwindPortIndex.Onna:
				case SailwindPortIndex.FireflyGrotto:
					return "(Letter got eject away before you can approach, falls into the seas)";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
