using UnityEngine;

public class ThornSolidPhysics : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        GetComponentInParent<AttributeThornWall>().NotifySolidCollision(collision);
    }
}