using UnityEngine;

namespace PostalExpansion
{
	internal sealed class SirenSongEmptyHouseMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly SirenSongEmptyHouseMission Instance = new();

		private SirenSongEmptyHouseMission()
			: base(
				"siren_song_empty_house",
				"Siren Song",
				SailwindPortIndex.SirenSong,
				new Vector3(16.545f, 1.400f, -21.654f),
				new Vector3(0f, 250.853f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Night,
				new[]
				{
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.Oasis,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.KiciaBay
				},
				"Empty house")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.HappyBay:
				case SailwindPortIndex.Oasis:
				case SailwindPortIndex.DragonCliffs:
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.KiciaBay:
					return "(You slip the letter under the door)";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
