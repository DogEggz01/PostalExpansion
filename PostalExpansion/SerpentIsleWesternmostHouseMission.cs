using UnityEngine;

namespace PostalExpansion
{
	internal sealed class SerpentIsleWesternmostHouseMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly SerpentIsleWesternmostHouseMission Instance = new();

		private SerpentIsleWesternmostHouseMission()
			: base(
				"serpent_isle_westernmost_house",
				"Serpent Isle",
				SailwindPortIndex.SerpentIsle,
				new Vector3(183.915f, 2.622f, -58.679f),
				new Vector3(0f, 171.934f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.NewPort,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.FireFishTown,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.SirenSong,
					SailwindPortIndex.Sunspire,
					SailwindPortIndex.FeyValley,
					SailwindPortIndex.Neverdin,
					SailwindPortIndex.OldAnkhTown,
					SailwindPortIndex.Oasis,
					SailwindPortIndex.Chronos
				},
				"Westernmost house")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.Chronos:
					return "They must be rich if they can take a vacation here.";

				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.NewPort:
				case SailwindPortIndex.DragonCliffs:
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.KiciaBay:
				case SailwindPortIndex.FireFishTown:
				case SailwindPortIndex.HappyBay:
				case SailwindPortIndex.SirenSong:
				case SailwindPortIndex.Sunspire:
				case SailwindPortIndex.FeyValley:
				case SailwindPortIndex.Neverdin:
				case SailwindPortIndex.OldAnkhTown:
				case SailwindPortIndex.Oasis:
					return "We'll check on cabin availability.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
