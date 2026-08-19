using UnityEngine;

namespace PostalExpansion
{
	internal sealed class HappyBayUnnamedGraveMission
		: AnonymousLetterMissionDefinition
	{
		internal static readonly HappyBayUnnamedGraveMission Instance = new();

		private HappyBayUnnamedGraveMission()
			: base(
				"happy_bay_unnamed_grave",
				"Happy Bay",
				SailwindPortIndex.HappyBay,
				new Vector3(-6791.194f, 55.928f, -5663.258f),
				new Vector3(0f, 355.344f, 0f),
				new Vector3(2.5f, 2.5f, 2.5f),
				8844.2f,
				new LetterDeliveryWindow(5f, 6f, "05-06"),
				new[]
				{
					SailwindPortIndex.Neverdin
				},
				"Unnamed grave")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			return GetOriginPortIndex(mission) == SailwindPortIndex.Neverdin
				? "(You place the letter on the grave. It fades into light when sun raise up)"
				: UnexpectedOrigin(mission);
		}
	}
}
