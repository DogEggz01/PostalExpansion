namespace PostalExpansion
{
	internal readonly struct LetterDeliveryWindow
	{
		internal static readonly LetterDeliveryWindow Day =
			new LetterDeliveryWindow(7f, 18f, "07-18");
		internal static readonly LetterDeliveryWindow Night =
			new LetterDeliveryWindow(19f, 5f, "19 - 05");

		internal LetterDeliveryWindow(
			float startHour,
			float endHour,
			string displayHours)
		{
			StartHour = startHour;
			EndHour = endHour;
			DisplayHours = displayHours;
		}

		internal float StartHour { get; }
		internal float EndHour { get; }
		internal string DisplayHours { get; }
	}

	internal static class LetterDeliveryHours
	{
		internal static bool IsOpen(LetterDeliveryWindow window)
		{
			if (Sun.sun == null)
			{
				return false;
			}

			float localTime = Sun.sun.localTime;
			return window.StartHour <= window.EndHour
				? localTime >= window.StartHour && localTime <= window.EndHour
				: localTime >= window.StartHour || localTime <= window.EndHour;
		}

		internal static string GetDisplayHours(LetterDeliveryWindow window)
		{
			return window.DisplayHours;
		}
	}
}
