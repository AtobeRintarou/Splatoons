using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;

public struct Splat
{
	public Matrix4x4 splatMatrix;
	public Vector4 channelMask;
	public Vector4 scaleBias;
}

public class SplatManagerSystem
{
	static SplatManagerSystem m_Instance;
	static public SplatManagerSystem instance
	{
		get
		{
			if (m_Instance == null)
            {
				m_Instance = new SplatManagerSystem();
			}
			return m_Instance;
		}
	}

	public int splatsX;
	public int splatsY;

	public Vector4 scores;

	// 描画されるスプラットのリスト
	internal List<Splat> m_Splats = new List<Splat>();
	
	public void AddSplat (Splat splat)
	{
		//Debug.Log ("Adding Splat");
		m_Splats.Add (splat);
	}

	// スプラットを描画するレンダラーの2dリスト
	internal List<List<Renderer>> m_RendererAray = new List<List<Renderer>> ();

	// スプラットを描画するレンダラーのリスト
	internal List<Renderer> m_Renderers = new List<Renderer>();


	public void AddRenderer (Renderer renderer)
	{
		while (renderer.lightmapIndex >= m_RendererAray.Count)
		{
			m_RendererAray.Add (new List<Renderer> ());
		}

		Debug.Log ("Adding Renderer");
		m_RendererAray [renderer.lightmapIndex].Add (renderer);
		m_Renderers.Add (renderer);
	}

}

public class SplatManager : MonoBehaviour
{
	public static SplatManager Instance;

	[SerializeField]
	public LightmapData lightmapData;

	[SerializeField]
	public LightmapSettings lightmapSettings;

	public int sizeX;
	public int sizeY;

	public Texture2D splatTexture;
	public int splatsX = 4;
	public int splatsY = 4;

	public List<RenderTexture> splatTexList;
	public List<RenderTexture> splatTexAltList;
	public List<RenderTexture> worldPosTexList;

	public RenderTexture splatTex;
	public RenderTexture splatTexAlt;

    public RenderTexture worldPosTex;
	public RenderTexture worldPosTexTemp;
	public RenderTexture worldTangentTex;
	public RenderTexture worldBinormalTex;
	private Camera rtCamera;

	private Material splatBlitMaterial;

	private bool evenFrame = false;

	public Vector4 scores = Vector4.zero;

	public RenderTexture scoreTex;
	public RenderTexture RT4;
	public Texture2D Tex4;

	// 重複したスプラット マネージャーが一度に有効になるのを防ぐ
	void Awake ()
	{

		if (SplatManager.Instance != null)
		{
			if (SplatManager.Instance != this)
			{
				Destroy (this);
			}
		}
		else
		{
			SplatManager.Instance = this;
		}

	}

	// これを初期化に使用
	void Start ()
	{

		SplatManagerSystem.instance.splatsX = splatsX;
		SplatManagerSystem.instance.splatsY = splatsY;

		splatBlitMaterial = new Material (Shader.Find ("Splatoonity/SplatBlit"));
		
		splatTex = new RenderTexture (sizeX, sizeY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
		splatTex.Create ();
		splatTexAlt = new RenderTexture (sizeX, sizeY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
		splatTexAlt.Create ();
		worldPosTex = new RenderTexture (sizeX, sizeY, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
		worldPosTex.Create ();
		worldPosTexTemp = new RenderTexture (sizeX, sizeY, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
		worldPosTexTemp.Create ();
		worldTangentTex = new RenderTexture (sizeX, sizeY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
		worldTangentTex.Create ();
		worldBinormalTex = new RenderTexture (sizeX, sizeY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
		worldBinormalTex.Create();

		splatTexList = new List<RenderTexture> ();
		splatTexAltList = new List<RenderTexture> ();
		worldPosTexList = new List<RenderTexture> ();

		Shader.SetGlobalTexture ("_SplatTex", splatTex);
		Shader.SetGlobalTexture ("_WorldPosTex", worldPosTex);
		Shader.SetGlobalTexture ("_WorldTangentTex", worldTangentTex);
		Shader.SetGlobalTexture ("_WorldBinormalTex", worldBinormalTex);
		Shader.SetGlobalVector ("_SplatTexSize", new Vector4 (sizeX, sizeY, 0, 0));


		// スコアを集計するためのテクスチャ
		// 最終的なスコアを維持するために 4x4 ldr テクスチャにミッピングされるため、より高い精度が必要
		scoreTex = new RenderTexture (sizeX/8, sizeY/8, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
		scoreTex.autoGenerateMips = true;
		scoreTex.useMipMap = true;
		scoreTex.Create ();
		RT4 = new RenderTexture (4, 4, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
		RT4.Create ();
		Tex4 = new Texture2D (4, 4, TextureFormat.ARGB32, false);

		
		GameObject rtCameraObject = new GameObject ();
		rtCameraObject.name = "rtCameraObject";
		rtCameraObject.transform.position = Vector3.zero;
		rtCameraObject.transform.rotation = Quaternion.identity;
		rtCameraObject.transform.localScale = Vector3.one;
		rtCamera = rtCameraObject.AddComponent<Camera> ();
		rtCamera.renderingPath = RenderingPath.Forward;
		rtCamera.clearFlags = CameraClearFlags.SolidColor;
		rtCamera.backgroundColor = new Color (0, 0, 0, 0);
		rtCamera.orthographic = true;
		rtCamera.nearClipPlane = 0.0f;
		rtCamera.farClipPlane = 1.0f;
		rtCamera.orthographicSize = 1.0f;
		rtCamera.aspect = 1.0f;
		rtCamera.useOcclusionCulling = false;
		rtCamera.enabled = false;

		RenderTextures ();
		BleedTextures ();
		StartCoroutine( UpdateScores() );

    }

	/*
	// シェーダー置換を使用してテクスチャをレンダリングする
	// ただし、これはシーン内のすべてのオブジェクトをレンダリングする
	// ただし、レイヤーに基づいて選別することはできる
	void RenderTextures()
	{

		Material worldPosMaterial = new Material (Shader.Find ("Splatoonity/WorldPosUnwrap"));
		Material worldNormalMaterial = new Material (Shader.Find ("Splatoonity/WorldNormalUnwrap"));

		rtCamera.targetTexture = worldPosTex;
		rtCamera.RenderWithShader (Shader.Find ("Splatoonity/WorldPosUnwrap"), null);

		rtCamera.targetTexture = worldTangentTex;
		rtCamera.RenderWithShader (Shader.Find ("Splatoonity/WorldTangentUnwrap"), null);

		rtCamera.targetTexture = worldBinormalTex;
		rtCamera.RenderWithShader (Shader.Find ("Splatoonity/WorldBinormalUnwrap"), null);
	}
	*/

	// バッファーコマンドを使用してテクスチャをレンダリング
	// これは、レイヤーを気にせずにレンダリングするオブジェクトを明示的に追加できる
	// シーンのチャンクに対して複数のインスタンスを持つこともできる
	void RenderTextures()
	{
		// culling mask を Nothing に設定して、レンダラーを明示的に描画できるようにする
		rtCamera.cullingMask = LayerMask.NameToLayer("Nothing");

		Material worldPosMaterial = new Material (Shader.Find ("Splatoonity/WorldPosUnwrap"));
		Material worldTangentMaterial = new Material (Shader.Find ("Splatoonity/WorldTangentUnwrap"));
		Material worldBiNormalMaterial = new Material (Shader.Find ("Splatoonity/WorldBinormalUnwrap"));

		// rendererd が必要なすべてのオブジェクトを収集し、DrawRenderer をループすることができる
		// ただし、この例では、1 つのレンダラーを描画しているだけ
		//Renderer envRenderer = this.gameObject.GetComponent<Renderer> ();

		int rendererCount = SplatManagerSystem.instance.m_Renderers.Count;

		// multi render target を使用して、各レンダラーを 1 回だけ描画することもできる
		CommandBuffer cb = new CommandBuffer();
		cb.SetRenderTarget(worldPosTex);
		cb.ClearRenderTarget(true, true, new Color(0,0,0,0) );
		//cb.DrawRenderer(envRenderer, worldPosMaterial);

		for (int i = 0; i < rendererCount; i++)
		{
			cb.DrawRenderer (SplatManagerSystem.instance.m_Renderers[i], worldPosMaterial);
		}

		cb.SetRenderTarget(worldTangentTex);
		cb.ClearRenderTarget(true, true, new Color(0,0,0,0) );
		//cb.DrawRenderer(envRenderer, worldTangentMaterial);

		for (int i = 0; i < rendererCount; i++)
		{
			cb.DrawRenderer (SplatManagerSystem.instance.m_Renderers[i], worldTangentMaterial);
		}

		cb.SetRenderTarget(worldBinormalTex);
		cb.ClearRenderTarget(true, true, new Color(0,0,0,0) );
		//cb.DrawRenderer(envRenderer, worldBiNormalMaterial);

		for (int i = 0; i < rendererCount; i++)
		{
			cb.DrawRenderer (SplatManagerSystem.instance.m_Renderers[i], worldBiNormalMaterial);
		}

		// カメラを 1 回レンダリングするだけで済む
		rtCamera.AddCommandBuffer (CameraEvent.AfterEverything, cb);
		rtCamera.Render ();
	}


	void BleedTextures()
	{
		Graphics.Blit (Texture2D.blackTexture, splatTex, splatBlitMaterial, 1);		
		Graphics.Blit (Texture2D.blackTexture, splatTexAlt, splatBlitMaterial, 1);

		splatBlitMaterial.SetVector("_SplatTexSize", new Vector2( sizeX, sizeY ) );

		// ワールド位置を 2pix 分ブリード Bleed する
		Graphics.Blit (worldPosTex, worldPosTexTemp, splatBlitMaterial, 2);
		Graphics.Blit (worldPosTexTemp, worldPosTex, splatBlitMaterial, 2);

		// こいつはもういらない
		worldPosTexTemp.Release();
		worldPosTexTemp = null;
	}


	// splats を Blit
	// 深さからワールド座標を取得する代わりに
	// テクスチャに格納されているワールド座標を使用
	// 各スプラットは、ワールド座標のテクスチャ全体に対してテストされる
	void PaintSplats()
	{
		if (SplatManagerSystem.instance.m_Splats.Count > 0)
		{
			Matrix4x4[] SplatMatrixArray = new Matrix4x4[10];
			Vector4[] SplatScaleBiasArray = new Vector4[10];
			Vector4[] SplatChannelMaskArray = new Vector4[10];

			// フレームごとに最大 10 個のスプラットをレンダリング
			int i = 0;

			while( SplatManagerSystem.instance.m_Splats.Count > 0 && i < 10 )
			{
				SplatMatrixArray [i] = SplatManagerSystem.instance.m_Splats [0].splatMatrix;
				SplatScaleBiasArray [i] = SplatManagerSystem.instance.m_Splats [0].scaleBias;
				SplatChannelMaskArray [i] = SplatManagerSystem.instance.m_Splats [0].channelMask;
				SplatManagerSystem.instance.m_Splats.RemoveAt(0);
				i++;
			}

			splatBlitMaterial.SetMatrixArray ( "_SplatMatrix", SplatMatrixArray );
			splatBlitMaterial.SetVectorArray ( "_SplatScaleBias", SplatScaleBiasArray );
			splatBlitMaterial.SetVectorArray ( "_SplatChannelMask", SplatChannelMaskArray );

			splatBlitMaterial.SetInt ( "_TotalSplats", i );

			splatBlitMaterial.SetTexture ("_WorldPosTex", worldPosTex);

			// 適切にスプラットをブレンドするために、バッファー間でピンポンする
			// これが計算シェーダーの場合、1 つのバッファーを更新するだけで済む

			if (evenFrame)
			{
				splatBlitMaterial.SetTexture ("_LastSplatTex", splatTexAlt);
				Graphics.Blit (splatTexture, splatTex, splatBlitMaterial, 0);
				Shader.SetGlobalTexture ("_SplatTex", splatTex);
				evenFrame = false;
			}
			else
			{
				splatBlitMaterial.SetTexture ("_LastSplatTex", splatTex);
				Graphics.Blit (splatTexture, splatTexAlt, splatBlitMaterial, 0);
				Shader.SetGlobalTexture ("_SplatTex", splatTexAlt);
				evenFrame = true;
			}
		}
	}

	// スプラットテクスチャを 4x4 テクスチャにミッピングし、
	// ピクセルをサンプリングして、スコアを更新
	// すべてがスムーズに実行されるように、操作全体を数フレームに分散させる
	// 毎秒 1 回だけスコアを更新
	IEnumerator UpdateScores()
	{
		while( true )
		{
			yield return new WaitForEndOfFrame();

			Graphics.Blit (splatTex, scoreTex, splatBlitMaterial, 3);
			Graphics.Blit (scoreTex, RT4, splatBlitMaterial, 4);

			RenderTexture.active = RT4;
			Tex4.ReadPixels (new Rect (0, 0, 4, 4), 0, 0);
			Tex4.Apply ();

			yield return new WaitForSeconds(0.01f);

			Color scoresColor = new Color(0,0,0,0);
			scoresColor += Tex4.GetPixel(0,0);
			scoresColor += Tex4.GetPixel(0,1);
			scoresColor += Tex4.GetPixel(0,2);
			scoresColor += Tex4.GetPixel(0,3);

			yield return new WaitForSeconds(0.01f);

			scoresColor += Tex4.GetPixel(1,0);
			scoresColor += Tex4.GetPixel(1,1);
			scoresColor += Tex4.GetPixel(1,2);
			scoresColor += Tex4.GetPixel(1,3);

			yield return new WaitForSeconds(0.01f);

			scoresColor += Tex4.GetPixel(2,0);
			scoresColor += Tex4.GetPixel(2,1);
			scoresColor += Tex4.GetPixel(2,2);
			scoresColor += Tex4.GetPixel(2,3);

			yield return new WaitForSeconds(0.01f);

			scoresColor += Tex4.GetPixel(3,0);
			scoresColor += Tex4.GetPixel(3,1);
			scoresColor += Tex4.GetPixel(3,2);
			scoresColor += Tex4.GetPixel(3,3);

			scores.x = scoresColor.r;
			scores.y = scoresColor.g;
			scores.z = scoresColor.b;
			scores.w = scoresColor.a;

			SplatManagerSystem.instance.scores = scores;

			yield return new WaitForSeconds (1.0f);
		}
	}
	
	void Update ()
	{
		PaintSplats ();
	}
}
