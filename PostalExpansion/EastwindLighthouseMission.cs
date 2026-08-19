using UnityEngine;

namespace PostalExpansion
{
	internal sealed class EastwindLighthouseMission
		: AnonymousLetterMissionDefinition
	{
		internal static readonly EastwindLighthouseMission Instance = new();

		private EastwindLighthouseMission()
			: base(
				"eastwind_lighthouse",
				"Eastwind",
				SailwindPortIndex.Eastwind,
				new Vector3(3070.795f, 8.574f, -4694.155f),
				new Vector3(0f, 266.217f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				5610.9f,
				new LetterDeliveryWindow(22f, 2f, "22-02"),
				new[]
				{
					SailwindPortIndex.Neverdin,
					SailwindPortIndex.Sanctuary,
					SailwindPortIndex.AestraAbbey,
					SailwindPortIndex.Chronos,
					SailwindPortIndex.FireFishTown,
					SailwindPortIndex.Oasis
				},
				"Light house")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.Oasis:
					return "Aye matey, Me belong 'ere now.";

				case SailwindPortIndex.Neverdin:
				case SailwindPortIndex.Sanctuary:
				case SailwindPortIndex.AestraAbbey:
				case SailwindPortIndex.Chronos:
				case SailwindPortIndex.FireFishTown:
					return "YER GODS 'AVE NO POWER 'ERE LANDLUBBER!";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
