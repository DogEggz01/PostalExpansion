using UnityEngine;

namespace PostalExpansion
{
	internal sealed class MountMaleficWheatManorMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly MountMaleficWheatManorMission Instance = new();

		private MountMaleficWheatManorMission()
			: base(
				"mount_malefic_wheat_manor",
				"Mount Malefic",
				SailwindPortIndex.MountMalefic,
				new Vector3(-105.357f, 0.931f, 172.698f),
				new Vector3(0f, 201.481f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Day,
				new[]
				{
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.Eastwind,
					SailwindPortIndex.FireflyGrotto,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.DragonCliffs,
					SailwindPortIndex.NewPort,
					SailwindPortIndex.KiciaBay
				},
				"Wheat manor")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.Eastwind:
				case SailwindPortIndex.FireflyGrotto:
					return "I'll arrange next shipment.";

				case SailwindPortIndex.HappyBay:
				case SailwindPortIndex.NewPort:
					return "I fully support the colonization effort!";

				case SailwindPortIndex.DragonCliffs:
				case SailwindPortIndex.KiciaBay:
					return "You ever taste those rice noodles? It's like eating a whip!";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
