namespace PostalExpansion
{
	internal static class VanillaQuestState
	{
		private const int HideoutQuestIndex = 0;
		private const int CompletedQuestState = -5;

		internal static bool IsHideoutQuestComplete()
		{
			Quests quests = Quests.instance;
			return quests != null &&
				quests.currentQuests != null &&
				quests.currentQuests.Length > HideoutQuestIndex &&
				quests.currentQuests[HideoutQuestIndex] == CompletedQuestState;
		}
	}
}
