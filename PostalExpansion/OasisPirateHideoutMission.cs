using UnityEngine;

namespace PostalExpansion
{
	internal sealed class OasisPirateHideoutMission
		: AnonymousLetterMissionDefinition
	{
		internal static readonly OasisPirateHideoutMission Instance = new();

		private OasisPirateHideoutMission()
			: base(
				"oasis_pirate_hideout",
				"Oasis",
				SailwindPortIndex.Oasis,
				new Vector3(-10609.600f, 146.177f, -6544.561f),
				new Vector3(0f, 251.320f, 0f),
				new Vector3(2f, 2f, 2f),
				12463.9f,
				new LetterDeliveryWindow(12f, 17f, "12-17"),
				new[]
				{
					SailwindPortIndex.SirenSong,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.Chronos,
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.HappyBay
				},
				"Shitface on bench")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			int originPortIndex = GetOriginPortIndex(mission);
			if (!VanillaQuestState.IsHideoutQuestComplete())
			{
				if (originPortIndex == SailwindPortIndex.Chronos)
				{
					return "Quite an old salt ain't ye? Ye should talk to Cap'n.";
				}

				switch (originPortIndex)
				{
					case SailwindPortIndex.SirenSong:
					case SailwindPortIndex.DragonCliffs:
					case SailwindPortIndex.KiciaBay:
					case SailwindPortIndex.GoldRockCity:
					case SailwindPortIndex.HappyBay:
						return "If ye ain't afraid o' gettin' yer feet wet, talk to Cap'n.";

					default:
						return UnexpectedOrigin(mission);
				}
			}

			switch (originPortIndex)
			{
				case SailwindPortIndex.SirenSong:
					return "Cap'n should get a better 'ouse in Fort Aestrin...";

				case SailwindPortIndex.DragonCliffs:
					return "Them scallywag better not be squiffy on leaves this time...";

				case SailwindPortIndex.KiciaBay:
					return "Pegleg got shitty writing, an' now the letter be wet...";

				case SailwindPortIndex.GoldRockCity:
					return "Fishy landlubber better 'ave jolly target this time...";

				case SailwindPortIndex.HappyBay:
					return "Me freedom fighters now, Heh Heh.";

				case SailwindPortIndex.Chronos:
					return "WE NEED A BIGGER SHIP CAP'N!";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
