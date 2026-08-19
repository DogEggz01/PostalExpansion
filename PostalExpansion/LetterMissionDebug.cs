using System.Collections.Generic;
using UnityEngine;

namespace PostalExpansion
{
	internal static class LetterMissionDebug
	{
		private const int EntriesPerPage = 5;
		private static int page;

		internal static void ShowSpawnLocations()
		{
			var statusLines = new List<string>();
			statusLines.AddRange(RegisteredLetterMissions.GetDebugStatusLines());
			statusLines.AddRange(AnonymousLetterMissions.GetDebugStatusLines());

			int pageCount = Mathf.Max(
				1,
				Mathf.CeilToInt((float)statusLines.Count / EntriesPerPage));
			page = Mathf.Clamp(page, 0, pageCount - 1);
			int startIndex = page * EntriesPerPage;
			int entriesOnPage = Mathf.Min(
				EntriesPerPage,
				statusLines.Count - startIndex);

			var pageLines = new List<string>
			{
				"Letter mission spawns (" + (page + 1) + "/" + pageCount + "):"
			};
			for (int i = 0; i < entriesOnPage; i++)
			{
				pageLines.Add(statusLines[startIndex + i]);
			}

			Debug.Log(
				"Postal Expansion: letter mission spawns: | " +
				string.Join(" | ", statusLines));
			if (NotificationUi.instance != null)
			{
				NotificationUi.instance.ShowNotification(
					string.Join("\n", pageLines));
			}

			page = (page + 1) % pageCount;
		}

		internal static void Reset()
		{
			page = 0;
		}
	}
}
