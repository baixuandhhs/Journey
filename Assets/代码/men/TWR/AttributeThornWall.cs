using UnityEngine;
using System.Collections;
using Cinemachine; // 必须引用

public class AttributeThornWall : MonoBehaviour
{
    [Header("=== 核心属性设置 ===")]
    public AttributeType requiredAttribute; // 开启此门需要的属性
    public Animator wallAnim;               // 引用带动画的子物体
    public Collider2D solidCollider;       // 引用带实体碰撞体的子物体

    [Header("=== 特写镜头同步 ===")]
    public CinemachineVirtualCamera cutsceneVcam; // 拖入特写虚拟相机
    public float cameraBlendTime = 1.5f;          // 匹配 Cinemachine Brain 的 Default Blend 时间
    public float cutsceneWaitTime = 1.5f;         // 门开完后让玩家看多久
    private bool hasTriggeredCutscene = false;    // 是否执行过第一次开门特写

    [Header("=== 荆棘伤害与音效 ===")]
    public float damage = 20f;
    public float knockbackForce = 12f;
    public AudioClip thornHitSound;        // 荆棘扎人音效


    public void NotifyTriggerEnter(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            CheckAndHandleAttribute(other.GetComponent<PlayerController>());
        }
    }

    public void NotifyTriggerStay(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            CheckAndHandleAttribute(other.GetComponent<PlayerController>());
        }
    }

    public void NotifyTriggerExit(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
           
            if (!hasTriggeredCutscene || cutsceneVcam.Priority == 0)
            {
                SetWallState(false);
            }
        }
    }

    private void CheckAndHandleAttribute(PlayerController pc)
    {
        if (pc == null) return;

        if (pc.currentAttribute == requiredAttribute)
        {
           
            if (!hasTriggeredCutscene)
            {
                StartCoroutine(PlayOpenCutscene(pc));
            }
            else if (cutsceneVcam.Priority == 0) 
            {
                SetWallState(true);
            }
        }
        else
        {
          
            if (!hasTriggeredCutscene || cutsceneVcam.Priority == 0)
            {
                SetWallState(false);
            }
        }
    }

    IEnumerator PlayOpenCutscene(PlayerController pc)
    {
        hasTriggeredCutscene = true;

       
        if (cutsceneVcam != null) cutsceneVcam.Priority = 20;

       
        pc.enabled = false;
        Rigidbody2D playerRb = pc.GetComponent<Rigidbody2D>();
        if (playerRb) playerRb.velocity = Vector2.zero;

     
        yield return new WaitForSeconds(cameraBlendTime);

        SetWallState(true);
        Debug.Log("<color=gold>镜头到位，开门动画开始</color>");

       
        yield return new WaitForSeconds(cutsceneWaitTime);

        
        if (cutsceneVcam != null) cutsceneVcam.Priority = 0;

        
        pc.enabled = true;
    }

    
    public void NotifySolidCollision(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            PlayerController pc = collision.collider.GetComponent<PlayerController>();
            PlayerStats stats = collision.collider.GetComponent<PlayerStats>();

            if (pc != null && pc.currentAttribute == requiredAttribute) return;

           
            if (stats != null && !stats.GetInvincible())
            {
                stats.TakeDamage(damage);
                pc.ApplyKnockbackLock();

                
                AudioSource playerAudio = pc.GetComponent<AudioSource>();
                if (playerAudio != null && thornHitSound != null)
                {
                    playerAudio.pitch = Random.Range(0.9f, 1.1f);
                    playerAudio.PlayOneShot(thornHitSound);
                }

                
                Vector2 dir = (collision.transform.position - transform.position).normalized;
                dir.y = 0.6f;
                Rigidbody2D rb = collision.collider.GetComponent<Rigidbody2D>();
                if (rb)
                {
                    rb.velocity = Vector2.zero;
                    rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
                }

               
                PlayerCombat combat = pc.GetComponent<PlayerCombat>();
                if (combat != null) combat.TriggerFeedback(0.05f, true);
            }
        }
    }

    void SetWallState(bool opened)
    {
        if (wallAnim != null) wallAnim.SetBool("IsOpen", opened);

        Collider2D[] allColliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in allColliders)
        {
            if (!col.isTrigger) col.enabled = !opened;
        }
    }
}