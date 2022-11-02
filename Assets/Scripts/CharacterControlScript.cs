using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
public class CharacterControlScript : MonoBehaviour
{
    //移動処理に必要なコンポーネントを設定
    public Animator animator;                 //モーションをコントロールするためAnimatorを取得
    public CharacterController controller;    //キャラクター移動を管理するためCharacterControllerを取得

    //移動速度等のパラメータ用変数(inspectorビューで設定)
    public float speed;         //キャラクターの移動速度
    public float jumpSpeed;     //キャラクターのジャンプ力
    public float rotateSpeed;   //キャラクターの方向転換速度
    public float gravity;       //キャラにかかる重力の大きさ

    Vector3 targetDirection;        //移動する方向のベクトル
    Vector3 moveDirection = Vector3.zero;

    Rigidbody rb;
    private Ray ray; // 飛ばすレイ
    private float distance = 0.5f; // レイを飛ばす距離
    private RaycastHit hit; // レイが何かに当たった時の情報
    private Vector3 rayPosition; // レイを発射する位置

    public RenderTexture RT4;
    public Texture2D Tex4;

    // Start関数は変数を初期化するための関数
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update関数は1フレームに１回実行される
    void Update()
    {
        moveControl();  //移動用関数
        RotationControl(); //旋回用関数

        //最終的な移動処理
        //(これが無いとCharacterControllerに情報が送られないため、動けない)
        controller.Move(moveDirection * Time.deltaTime);

        rayPosition = transform.position + new Vector3(0, 0.5f, 0); // レイを発射する位置の調整
        ray = new Ray(rayPosition, transform.up * -1); // レイを下に飛ばす
        Debug.DrawRay(ray.origin, ray.direction * distance, Color.red); // レイを赤色で表示させる

        if (Physics.Raycast(ray, out hit, distance)) // レイが当たった時の処理
        {
            if (hit.collider.tag == "Ground") // レイが地面に触れたら、
            {

            }

            else // そうでなければ、
            {

            }
        }
    }

    void moveControl()
    {
        //★進行方向計算
        //キーボード入力を取得
        float v = Input.GetAxisRaw("Vertical");         //InputManagerの↑↓の入力       
        float h = Input.GetAxisRaw("Horizontal");       //InputManagerの←→の入力

        //カメラの正面方向ベクトルからY成分を除き、正規化してキャラが走る方向を取得
        Vector3 forward = Vector3.Scale(Camera.main.transform.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 right = Camera.main.transform.right; //カメラの右方向を取得

        //カメラの方向を考慮したキャラの進行方向を計算
        targetDirection = h * right + v * forward;

        //★地上にいる場合の処理
        if (controller.isGrounded)
        {
            //移動のベクトルを計算
            moveDirection = targetDirection * speed;

            //Jumpボタンでジャンプ処理
            if (Input.GetButton("Jump"))
            {
                moveDirection.y = jumpSpeed;
            }
        }
        else        //空中操作の処理（重力加速度等）
        {
            float tempy = moveDirection.y;
            //(↓の２文の処理があると空中でも入力方向に動けるようになる)
            moveDirection = Vector3.Scale(targetDirection, new Vector3(1, 0, 1)).normalized; //◆ コメントアウト解除
            moveDirection *= speed; //◆ コメントアウト解除
            moveDirection.y = tempy - gravity * Time.deltaTime;
        }

        //★走行アニメーション管理
        if (v > .1 || v < -.1 || h > .1 || h < -.1) //(移動入力があると)
        {
            //animator.SetFloat("Speed", 1f); //キャラ走行のアニメーションON
        }
        else    //(移動入力が無いと)
        {
            //animator.SetFloat("Speed", 0f); //キャラ走行のアニメーションOFF
        }
    }

    void RotationControl()  //キャラクターが移動方向を変えるときの処理
    {
        Vector3 rotateDirection = moveDirection;
        rotateDirection.y = 0;

        //それなりに移動方向が変化する場合のみ移動方向を変える
        if (rotateDirection.sqrMagnitude > 0.01)
        {
            //緩やかに移動方向を変える
            float step = rotateSpeed * Time.deltaTime;
            Vector3 newDir = Vector3.Slerp(transform.forward, rotateDirection, step);
            transform.rotation = Quaternion.LookRotation(newDir);
        }
    }

    IEnumerator UpdateScores()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();

            //Graphics.Blit(splatTex, scoreTex, splatBlitMaterial, 3);
            //Graphics.Blit(scoreTex, RT4, splatBlitMaterial, 4);

            RenderTexture.active = RT4;
            Tex4.ReadPixels(new Rect(0, 0, 4, 4), 0, 0);
            Tex4.Apply();

            yield return new WaitForSeconds(0.01f);

            Color scoresColor = new Color(0, 0, 0, 0);
            scoresColor += Tex4.GetPixel(0, 0);
            scoresColor += Tex4.GetPixel(0, 1);
            scoresColor += Tex4.GetPixel(0, 2);
            scoresColor += Tex4.GetPixel(0, 3);

            yield return new WaitForSeconds(0.01f);

            scoresColor += Tex4.GetPixel(1, 0);
            scoresColor += Tex4.GetPixel(1, 1);
            scoresColor += Tex4.GetPixel(1, 2);
            scoresColor += Tex4.GetPixel(1, 3);

            yield return new WaitForSeconds(0.01f);

            scoresColor += Tex4.GetPixel(2, 0);
            scoresColor += Tex4.GetPixel(2, 1);
            scoresColor += Tex4.GetPixel(2, 2);
            scoresColor += Tex4.GetPixel(2, 3);

            yield return new WaitForSeconds(0.01f);

            scoresColor += Tex4.GetPixel(3, 0);
            scoresColor += Tex4.GetPixel(3, 1);
            scoresColor += Tex4.GetPixel(3, 2);
            scoresColor += Tex4.GetPixel(3, 3);

            yield return new WaitForSeconds(1.0f);
        }
    }
}