using UnityEngine;

namespace PostalExpansion
{
	internal sealed class NeverdinTempleMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly NeverdinTempleMission Instance = new();

		private NeverdinTempleMission()
			: base(
				"neverdin_temple",
				"Neverdin",
				SailwindPortIndex.Neverdin,
				new Vector3(-11.756f, 3.429f, 148.984f),
				new Vector3(0f, 4.207f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.Sanctuary,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.Oasis,
					SailwindPortIndex.MirageMountain,
					SailwindPortIndex.KiciaBay
				},
				"Temple")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.KiciaBay:
				case SailwindPortIndex.DragonCliffs:
					return "They need the guidance of the sun.";

				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.Oasis:
					return "Sabbih Ash-shams!";

				case SailwindPortIndex.MirageMountain:
					return "The sun gives, and the sun takes away.";

				case SailwindPortIndex.Sanctuary:
					return "This interfaith dialogue is inspiring.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
