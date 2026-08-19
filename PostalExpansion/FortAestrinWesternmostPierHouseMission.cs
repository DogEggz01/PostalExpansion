using UnityEngine;

namespace PostalExpansion
{
	internal sealed class FortAestrinWesternmostPierHouseMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly FortAestrinWesternmostPierHouseMission Instance = new();

		private FortAestrinWesternmostPierHouseMission()
			: base(
				"fort_aestrin_westernmost_house_at_pier",
				"Fort Aestrin",
				SailwindPortIndex.FortAestrin,
				new Vector3(-144.465f, -1.242f, -99.124f),
				new Vector3(359.794f, 152.539f, 359.474f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Night,
				new[]
				{
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.Neverdin,
					SailwindPortIndex.MirageMountain,
					SailwindPortIndex.OldAnkhTown
				},
				"Westernmost house on the pier")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.Neverdin:
				case SailwindPortIndex.MirageMountain:
				case SailwindPortIndex.OldAnkhTown:
					return "Sabbih Ash-shams!";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
