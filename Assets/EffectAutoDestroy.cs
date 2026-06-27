using UnityEngine;

public class EffectAutoDestroy : MonoBehaviour
{
   
    void Start()
    {
       
        Destroy(gameObject, 1.5f);
    }

  
    void Update()
    {
    }
}