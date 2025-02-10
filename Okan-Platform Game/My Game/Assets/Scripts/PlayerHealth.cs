using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public PlayerDataWithDash Data;
    [HideInInspector] public int currentHealth;
    public Transform respawnPoint;  
    public HealthBar healthBar;
    private DamageFlash damageFlash;
    [SerializeField] ParticleSystem particleSystem;
    
    void Start()
    {
        currentHealth = Data.maxHealth;
        healthBar.SetMaxHealth(Data.maxHealth);
        damageFlash = GetComponent<DamageFlash>();
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.U))
        {
            TakeDamage(1);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        damageFlash.CallDamageFlash();
        healthBar.SetHealth(currentHealth);
        particleSystem.Play();

        if(currentHealth <= 0)
        {
            gameObject.transform.position = respawnPoint.position;
            currentHealth = Data.maxHealth;
            healthBar.SetMaxHealth(Data.maxHealth);
        }
    }
}
