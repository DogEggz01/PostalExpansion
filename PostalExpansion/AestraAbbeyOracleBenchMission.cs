using UnityEngine;

namespace PostalExpansion
{
	internal sealed class AestraAbbeyOracleBenchMission
		: AnonymousLetterMissionDefinition
	{
		internal static readonly AestraAbbeyOracleBenchMission Instance = new();

		private AestraAbbeyOracleBenchMission()
			: base(
				"aestra_abbey_oracle_bench",
				"Aestra Abbey",
				SailwindPortIndex.AestraAbbey,
				new Vector3(-129.328f, -1.928f, 1355.791f),
				new Vector3(0f, 257.793f, 0f),
				new Vector3(2.5f, 2.5f, 2.5f),
				1362.3f,
				new LetterDeliveryWindow(19f, 7f, "19-07"),
				new[]
				{
					SailwindPortIndex.Onna,
					SailwindPortIndex.TurtleIsland,
					SailwindPortIndex.FeyValley
				},
				"Oracle bench")
		{
		}

		internal override string GetDeliveryDialogue(Mission mission)
		{
			switch (GetOriginPortIndex(mission))
			{
				case SailwindPortIndex.Onna:
					return "(Letter open by itself once you approach one of the shrooms, showing totemic pattern before flying away. Closest shroom is gone)";

				case SailwindPortIndex.TurtleIsland:
					return "(The letter tries to let out the light but it got put out by shrooms quickly.)";

				case SailwindPortIndex.FeyValley:
					return "(You place the letter on bench. You heard a pleasent chime.)";

				default:
					return UnexpectedOrigin(mission);
			}
		}
	}
}
