using UnityEngine;

namespace PostalExpansion
{
	internal sealed class FireflyGrottoChurchMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly FireflyGrottoChurchMission Instance = new();

		private FireflyGrottoChurchMission()
			: base(
				"firefly_grotto_church",
				"Firefly Grotto",
				SailwindPortIndex.FireflyGrotto,
				new Vector3(128.273f, 5.458f, 37.452f),
				new Vector3(0f, 107.820f, 0f),
				new Vector3(2.5f, 2.5f, 2.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.AestraAbbey,
					SailwindPortIndex.NewPort,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.DragonCliffs
				},
				"Chruch")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.AestraAbbey:
					return "Ora et Labora.";

				case SailwindPortIndex.NewPort:
				case SailwindPortIndex.HappyBay:
				case SailwindPortIndex.DragonCliffs:
					return "We must continued our missionary work.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
