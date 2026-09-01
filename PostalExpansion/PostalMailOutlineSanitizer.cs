using cakeslice;
using UnityEngine;

namespace PostalExpansion
{
	internal sealed class PostalMailOutlineSanitizer : MonoBehaviour
	{
		internal static void Attach(GameObject prefab)
		{
			if (prefab != null && prefab.GetComponent<PostalMailOutlineSanitizer>() == null)
			{
				prefab.AddComponent<PostalMailOutlineSanitizer>();
			}
		}

		private void Awake()
		{
			// Runtime directory templates already contain the Outline added by the
			// donor's GoPointerButton.Start. Remove copied outlines before this
			// clone's own Start creates the one GoPointerButton controls.
			foreach (Outline outline in GetComponents<Outline>())
			{
				Object.DestroyImmediate(outline);
			}
		}
	}
}
