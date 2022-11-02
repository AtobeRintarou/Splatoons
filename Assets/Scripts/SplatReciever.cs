using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplatReciever : MonoBehaviour
{
	void Awake()
	{
		Renderer thisRenderer = this.gameObject.GetComponent<Renderer> ();

		if (thisRenderer != null)
		{
			SplatManagerSystem.instance.AddRenderer (thisRenderer);
		}
	}
}
