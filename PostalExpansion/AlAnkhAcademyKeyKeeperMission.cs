using UnityEngine;

namespace PostalExpansion
{
	internal sealed class AlAnkhAcademyKeyKeeperMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly AlAnkhAcademyKeyKeeperMission Instance = new();

		private AlAnkhAcademyKeyKeeperMission()
			: base(
				"al_ankh_academy_key_keeper",
				"Al'Ankh Academy",
				SailwindPortIndex.AlAnkhAcademy,
				new Vector3(-0.173f, 0.833f, 1.175f),
				new Vector3(0f, 170.709f, 0f),
				new Vector3(2f, 2f, 2f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.GoldRockCity,
					SailwindPortIndex.Oasis,
					SailwindPortIndex.MirageMountain,
					SailwindPortIndex.Sunspire,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.Sanctuary,
					SailwindPortIndex.Senna,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.FireFishTown,
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.Chronos
				},
				"Key Keeper")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.GoldRockCity:
				case SailwindPortIndex.Oasis:
				case SailwindPortIndex.DragonCliffs:
				case SailwindPortIndex.KiciaBay:
				case SailwindPortIndex.FireFishTown:
				case SailwindPortIndex.FortAestrin:
					return "Endless applications... I need a day off.";

				case SailwindPortIndex.MirageMountain:
					return "Oh! Ibn Doraid will be interested in this!";

				case SailwindPortIndex.Sunspire:
					return "These glasses cost a big fortune for sure.";

				case SailwindPortIndex.Sanctuary:
				case SailwindPortIndex.Senna:
					return "These ideas are... interesting.";

				case SailwindPortIndex.Chronos:
					return "(Whistles) Your journey must have been eventful.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
