using UnityEngine;

namespace PostalExpansion
{
	internal sealed class AlbacoreTownCreepyGuyMission
		: RegisteredLetterMissionDefinition
	{
		internal static readonly AlbacoreTownCreepyGuyMission Instance = new();

		private AlbacoreTownCreepyGuyMission()
			: base(
				"albacore_town_creepy_guy",
				"Albacore Town",
				SailwindPortIndex.AlbacoreTown,
				new Vector3(40.721f, 3.318f, -50.810f),
				new Vector3(0f, 55.492f, 0f),
				new Vector3(1.5f, 1.5f, 1.5f),
				LetterDeliveryWindow.Night,
				new[]
				{
					SailwindPortIndex.FortAestrin,
					SailwindPortIndex.NewPort,
					SailwindPortIndex.Senna
				},
				"Creepy guy")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.FortAestrin:
				case SailwindPortIndex.NewPort:
					return "Were you seen? You'd better not.";

				case SailwindPortIndex.Senna:
					return "This makes me homesick... but I need to know where the fishing ground is.";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
