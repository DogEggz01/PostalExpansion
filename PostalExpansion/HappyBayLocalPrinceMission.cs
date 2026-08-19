using UnityEngine;

namespace PostalExpansion
{
	internal sealed class HappyBayLocalPrinceMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly HappyBayLocalPrinceMission Instance = new();

		private HappyBayLocalPrinceMission()
			: base(
				"happy_bay_local_prince",
				"Happy Bay",
				SailwindPortIndex.HappyBay,
				new Vector3(102.092f, 17.839f, -23.281f),
				new Vector3(0f, 81.201f, 0f),
				new Vector3(3f, 3f, 3f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.NewPort,
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.AestraAbbey,
					SailwindPortIndex.FireflyGrotto,
					SailwindPortIndex.Chronos,
					SailwindPortIndex.Neverdin,
					SailwindPortIndex.Oasis
				},
				"Local Prince")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.NewPort:
					return "More demands...";

				case SailwindPortIndex.AestraAbbey:
				case SailwindPortIndex.FireflyGrotto:
				case SailwindPortIndex.Neverdin:
					return "Take your god or whatever somewhere else.";

				case SailwindPortIndex.DragonCliffs:
				case SailwindPortIndex.KiciaBay:
					return "These merchants only care about gold.";

				case SailwindPortIndex.GoldRockCity:
					return "A surprise, but a welcome one.";

				case SailwindPortIndex.Chronos:
					return "At least they stay away from this.";

				case SailwindPortIndex.Oasis:
					return VanillaQuestState.IsHideoutQuestComplete()
						? "Nice doing business with you."
						: "You're going to have a hard time on the return trip.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
