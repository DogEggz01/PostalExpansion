using UnityEngine;

namespace PostalExpansion
{
	internal sealed class DragonCliffsTeaMerchantHouseMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly DragonCliffsTeaMerchantHouseMission Instance = new();

		private DragonCliffsTeaMerchantHouseMission()
			: base(
				"dragon_cliffs_house_behind_tea_merchant",
				"Dragon Cliffs",
				SailwindPortIndex.DragonCliffs,
				new Vector3(-6.196f, 2.711f, -52.022f),
				new Vector3(0f, 101.025f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Night,
				new[]
				{
					SailwindPortIndex.Oasis,
					SailwindPortIndex.HappyBay,
					SailwindPortIndex.KiciaBay,
					SailwindPortIndex.SirenSong,
					SailwindPortIndex.GoldRockCity
				},
				"House behind tea merchant")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.Oasis:
					return VanillaQuestState.IsHideoutQuestComplete()
						? "New order, matey?"
						: "Thank you.";

				case SailwindPortIndex.HappyBay:
				case SailwindPortIndex.KiciaBay:
				case SailwindPortIndex.SirenSong:
				case SailwindPortIndex.GoldRockCity:
					return VanillaQuestState.IsHideoutQuestComplete()
						? "Which scurvy dog brings ye 'ere?"
						: "...You can leave now.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
