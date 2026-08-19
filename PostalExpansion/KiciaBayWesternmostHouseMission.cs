using UnityEngine;

namespace PostalExpansion
{
	internal sealed class KiciaBayWesternmostHouseMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly KiciaBayWesternmostHouseMission Instance = new();

		private KiciaBayWesternmostHouseMission()
			: base(
				"kicia_bay_westernmost_house",
				"Kicia Bay",
				SailwindPortIndex.KiciaBay,
				new Vector3(204.082f, 3.362f, -107.532f),
				new Vector3(0f, 74.864f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Night,
				new[]
				{
					SailwindPortIndex.Oasis,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.SirenSong,
					SailwindPortIndex.HappyBay
				},
				"Western most house")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			int originPortIndex = GetOriginPortIndex(mission);
			if (!VanillaQuestState.IsHideoutQuestComplete())
			{
				switch (originPortIndex)
				{
					case SailwindPortIndex.Oasis:
					case SailwindPortIndex.GoldRockCity:
						return "I wish for a dryer air...";

					case SailwindPortIndex.DragonCliffs:
					case SailwindPortIndex.SirenSong:
					case SailwindPortIndex.HappyBay:
						return "Oranges...Always oranges...";

					default:
						return UnexpectedOrigin(mission);
				}
			}

			switch (originPortIndex)
			{
				case SailwindPortIndex.Oasis:
					return "Aye...more work! Ye should be shipwreck in the fog!";

				case SailwindPortIndex.DragonCliffs:
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.SirenSong:
				case SailwindPortIndex.HappyBay:
					return "They just want me blue tobacco, feck off!";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
