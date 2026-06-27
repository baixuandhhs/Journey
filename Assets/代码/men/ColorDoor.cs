using UnityEngine;

public class ColorDoor : MonoBehaviour
{
    public AttributeType requiredAttribute;
    public Animator doorAnim;
    public Collider2D physicsCollider;

    private bool playerInRange = false;
    private PlayerController player;

    void Update()
    {
        if (playerInRange && player != null)
        {
          

            if (player.currentAttribute == requiredAttribute)
            {
                OpenDoor(true);
            }
            else
            {
                OpenDoor(false);
            }
        }
    }

    void OpenDoor(bool open)
    {
        if (doorAnim != null) doorAnim.SetBool("IsOpen", open);
        if (physicsCollider != null) physicsCollider.enabled = !open;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        
        Debug.Log("检测到碰撞，物体名称: " + other.name + " 标签: " + other.tag);

        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            player = other.GetComponent<PlayerController>();
            Debug.Log("<color=green>成功识别到玩家！</color>");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            OpenDoor(false);
            Debug.Log("玩家离开范围");
        }
    }
}