using UnityEngine;
using Object = UnityEngine.Object;

namespace PostalExpansion
{
	internal sealed class PostalMailTabButton : GPButtonMissionListWorld
	{
		public override void Start()
		{
			Component clonedOutline = GetComponent("Outline");
			if (clonedOutline != null)
			{
				if (clonedOutline is Behaviour outlineBehaviour)
				{
					outlineBehaviour.enabled = false;
				}
				Object.Destroy(clonedOutline);
			}

			base.Start();
		}

		public override void OnActivate()
		{
			PostalMissionUi.SetMailMissions(true);
			MissionListUI.instance.ResetPage();
		}
	}
}
