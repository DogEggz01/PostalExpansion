using UnityEngine;

namespace PostalExpansion
{
	internal sealed class AlchemistsIslandApprenticeMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly AlchemistsIslandApprenticeMission Instance = new();

		private AlchemistsIslandApprenticeMission()
			: base(
				"alchemists_island_apprentice",
				"Alchemist's Island",
				SailwindPortIndex.AlchemistsIsland,
				new Vector3(-0.235f, 0.336f, 14.538f),
				new Vector3(0f, 35.018f, 0f),
				new Vector3(3f, 3f, 3f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.Oasis,
					SailwindPortIndex.MirageMountain,
					SailwindPortIndex.AlAnkhAcademy,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.Sanctuary,
					SailwindPortIndex.Senna,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.Eastwind,
					SailwindPortIndex.FireflyGrotto,
					SailwindPortIndex.Chronos
				},
				"Alchemist's apprentice")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.Oasis:
				case SailwindPortIndex.MirageMountain:
				case SailwindPortIndex.HappyBay:
				case SailwindPortIndex.DragonCliffs:
				case SailwindPortIndex.KiciaBay:
					return "I will inform my mentor about this. Thank you.";

				case SailwindPortIndex.AlAnkhAcademy:
					return "This again... When will they stop trying?";

				case SailwindPortIndex.Sanctuary:
				case SailwindPortIndex.Senna:
					return "That's... a brilliant idea! I need to tell my mentor now!";

				case SailwindPortIndex.Eastwind:
				case SailwindPortIndex.FireflyGrotto:
					return "Your God cannot help the exhausted miners? Heh.";

				case SailwindPortIndex.Chronos:
					return "...Really?";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
