using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    [SerializeField] private int damage;

    private void OnTriggerEnter(Collider collider)
    {
        if(collider.tag == "Player")
            collider.GetComponent<PlayerHealth>().TakeDamage(damage);
    }
}
