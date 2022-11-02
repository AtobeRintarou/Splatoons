using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
public class CharacterControlScript : MonoBehaviour
{
    //�ړ������ɕK�v�ȃR���|�[�l���g��ݒ�
    public Animator animator;                 //���[�V�������R���g���[�����邽��Animator���擾
    public CharacterController controller;    //�L�����N�^�[�ړ����Ǘ����邽��CharacterController���擾

    //�ړ����x���̃p�����[�^�p�ϐ�(inspector�r���[�Őݒ�)
    public float speed;         //�L�����N�^�[�̈ړ����x
    public float jumpSpeed;     //�L�����N�^�[�̃W�����v��
    public float rotateSpeed;   //�L�����N�^�[�̕����]�����x
    public float gravity;       //�L�����ɂ�����d�͂̑傫��

    Vector3 targetDirection;        //�ړ���������̃x�N�g��
    Vector3 moveDirection = Vector3.zero;

    Rigidbody rb;
    private Ray ray; // ��΂����C
    private float distance = 0.5f; // ���C���΂�����
    private RaycastHit hit; // ���C�������ɓ����������̏��
    private Vector3 rayPosition; // ���C�𔭎˂���ʒu

    public RenderTexture RT4;
    public Texture2D Tex4;

    // Start�֐��͕ϐ������������邽�߂̊֐�
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update�֐���1�t���[���ɂP����s�����
    void Update()
    {
        moveControl();  //�ړ��p�֐�
        RotationControl(); //����p�֐�

        //�ŏI�I�Ȉړ�����
        //(���ꂪ������CharacterController�ɏ�񂪑����Ȃ����߁A�����Ȃ�)
        controller.Move(moveDirection * Time.deltaTime);

        rayPosition = transform.position + new Vector3(0, 0.5f, 0); // ���C�𔭎˂���ʒu�̒���
        ray = new Ray(rayPosition, transform.up * -1); // ���C�����ɔ�΂�
        Debug.DrawRay(ray.origin, ray.direction * distance, Color.red); // ���C��ԐF�ŕ\��������

        if (Physics.Raycast(ray, out hit, distance)) // ���C�������������̏���
        {
            if (hit.collider.tag == "Ground") // ���C���n�ʂɐG�ꂽ��A
            {

            }

            else // �����łȂ���΁A
            {

            }
        }
    }

    void moveControl()
    {
        //���i�s�����v�Z
        //�L�[�{�[�h���͂��擾
        float v = Input.GetAxisRaw("Vertical");         //InputManager�́����̓���       
        float h = Input.GetAxisRaw("Horizontal");       //InputManager�́����̓���

        //�J�����̐��ʕ����x�N�g������Y�����������A���K�����ăL����������������擾
        Vector3 forward = Vector3.Scale(Camera.main.transform.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 right = Camera.main.transform.right; //�J�����̉E�������擾

        //�J�����̕������l�������L�����̐i�s�������v�Z
        targetDirection = h * right + v * forward;

        //���n��ɂ���ꍇ�̏���
        if (controller.isGrounded)
        {
            //�ړ��̃x�N�g�����v�Z
            moveDirection = targetDirection * speed;

            //Jump�{�^���ŃW�����v����
            if (Input.GetButton("Jump"))
            {
                moveDirection.y = jumpSpeed;
            }
        }
        else        //�󒆑���̏����i�d�͉����x���j
        {
            float tempy = moveDirection.y;
            //(���̂Q���̏���������Ƌ󒆂ł����͕����ɓ�����悤�ɂȂ�)
            moveDirection = Vector3.Scale(targetDirection, new Vector3(1, 0, 1)).normalized; //�� �R�����g�A�E�g����
            moveDirection *= speed; //�� �R�����g�A�E�g����
            moveDirection.y = tempy - gravity * Time.deltaTime;
        }

        //�����s�A�j���[�V�����Ǘ�
        if (v > .1 || v < -.1 || h > .1 || h < -.1) //(�ړ����͂������)
        {
            //animator.SetFloat("Speed", 1f); //�L�������s�̃A�j���[�V����ON
        }
        else    //(�ړ����͂�������)
        {
            //animator.SetFloat("Speed", 0f); //�L�������s�̃A�j���[�V����OFF
        }
    }

    void RotationControl()  //�L�����N�^�[���ړ�������ς���Ƃ��̏���
    {
        Vector3 rotateDirection = moveDirection;
        rotateDirection.y = 0;

        //����Ȃ�Ɉړ��������ω�����ꍇ�݈̂ړ�������ς���
        if (rotateDirection.sqrMagnitude > 0.01)
        {
            //�ɂ₩�Ɉړ�������ς���
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