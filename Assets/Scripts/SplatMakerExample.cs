using UnityEngine;
using System.Collections;

public class SplatMakerExample : MonoBehaviour
{
	Vector4 channelMask = new Vector4(1,0,0,0);

	int splatsX = 1;
	int splatsY = 1;

	public float splatScale = 1.0f;

	void Start()
	{
	
	}
	
	void Update()
	{
		// スプラットアトラスに含まれるスプラットの数を取得する
		splatsX = SplatManagerSystem.instance.splatsX;
		splatsY = SplatManagerSystem.instance.splatsY;

		if( Input.GetKeyDown (KeyCode.Alpha1) )
		{
			channelMask = new Vector4(1,0,0,0);
		}
		
		if( Input.GetKeyDown (KeyCode.Alpha2) )
		{
			channelMask = new Vector4(0,1,0,0);
		}
		
		if( Input.GetKeyDown (KeyCode.Alpha3) )
		{
			channelMask = new Vector4(0,0,1,0);
		}
		
		if( Input.GetKeyDown (KeyCode.Alpha4) )
		{
			channelMask = new Vector4(0,0,0,1);
		}

		// カメラからマウスポインターに ray を放ち、そこにスプラットを描く
		// これは 4x4 スプラット アトラスのランダムなスケールとバイアスを選択するだけ
		// スプラット テクスチャのより大きなアトラスを使用して、使用する特定のスプラットのスケールとオフセットを選択できる
		if (Input.GetMouseButton (0))
		{
			
			Ray ray = Camera.main.ScreenPointToRay( Input.mousePosition );
			RaycastHit hit;

			if( Physics.Raycast( ray, out hit, 10000 ) )
			{
				Vector3 leftVec = Vector3.Cross ( hit.normal, Vector3.up );
				float randScale = Random.Range(0.5f,1.5f);
				
				GameObject newSplatObject = new GameObject();
				newSplatObject.transform.position = hit.point;

				if( leftVec.magnitude > 0.001f )
				{
					newSplatObject.transform.rotation = Quaternion.LookRotation( leftVec, hit.normal );
				}

				newSplatObject.transform.RotateAround( hit.point, hit.normal, Random.Range(-180, 180 ) );
				newSplatObject.transform.localScale = new Vector3( randScale, randScale * 0.5f, randScale ) * splatScale;

				Splat newSplat;
				newSplat.splatMatrix = newSplatObject.transform.worldToLocalMatrix;
				newSplat.channelMask = channelMask;

				float splatscaleX = 1.0f / splatsX;
				float splatscaleY = 1.0f / splatsY;
				float splatsBiasX = Mathf.Floor( Random.Range(0,splatsX * 0.99f) ) / splatsX;
				float splatsBiasY = Mathf.Floor( Random.Range(0,splatsY * 0.99f) ) / splatsY;

				newSplat.scaleBias = new Vector4(splatscaleX, splatscaleY, splatsBiasX, splatsBiasY );

				SplatManagerSystem.instance.AddSplat (newSplat);

				GameObject.Destroy( newSplatObject );
			}
		}
	}
}
