using UnityEngine;

public class ThornDetectionZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other) => GetComponentInParent<AttributeThornWall>().NotifyTriggerEnter(other);
    private void OnTriggerStay2D(Collider2D other) => GetComponentInParent<AttributeThornWall>().NotifyTriggerStay(other);
    private void OnTriggerExit2D(Collider2D other) => GetComponentInParent<AttributeThornWall>().NotifyTriggerExit(other);
}