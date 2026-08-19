using System;
using System.Collections.Generic;
using UnityEngine;

namespace PostalExpansion
{
	internal sealed class LetterMissionRegistry<TDefinition>
		where TDefinition : LetterMissionDefinition
	{
		private readonly List<TDefinition> definitions =
			new List<TDefinition>();

		internal LetterMissionRegistry(
			string categoryName,
			params TDefinition[] candidates)
		{
			var definitionIds = new HashSet<string>(StringComparer.Ordinal);
			foreach (TDefinition candidate in candidates)
			{
				if (candidate == null || string.IsNullOrEmpty(candidate.Id))
				{
					Debug.LogError(
						"Postal Expansion: ignored a " + categoryName +
						" mission without an ID.");
					continue;
				}

				if (!definitionIds.Add(candidate.Id))
				{
					Debug.LogError(
						"Postal Expansion: ignored duplicate " + categoryName +
						" mission ID " + candidate.Id + ".");
					continue;
				}

				definitions.Add(candidate);
			}
		}

		internal IReadOnlyList<TDefinition> All => definitions;
	}

	internal static class LetterMissionDefinitions
	{
		private static readonly IReadOnlyList<LetterMissionDefinition> Definitions =
			BuildDefinitions();

		internal static IReadOnlyList<LetterMissionDefinition> All => Definitions;

		private static IReadOnlyList<LetterMissionDefinition> BuildDefinitions()
		{
			var definitions = new List<LetterMissionDefinition>();
			foreach (RegisteredLetterMissionDefinition definition in
				RegisteredLetterMissionRegistry.All)
			{
				definitions.Add(definition);
			}

			foreach (AnonymousLetterMissionDefinition definition in
				AnonymousLetterMissionRegistry.All)
			{
				definitions.Add(definition);
			}

			return definitions.AsReadOnly();
		}
	}
}
