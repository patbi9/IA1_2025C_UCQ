using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class BaseEnemy : MonoBehaviour
{
    [Header("Vidas")] 
    public int maxLives = 3;
    public float invulnerableTime = 1f;

    [Header("Daño por colisión")] 
    public string damageTag = "Bubble"; //layer del objeto que daña
    public bool useTriggers = false;

    [Header("Apariencia")] 
    public Material[] materialsByLives;

    private int lives;
    private bool invulnerable;
    private Renderer rend;

    void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
        lives = Mathf.Max(1, maxLives);

        ApplyAppearance();
    }
    
    //colision
    void OnCollisionEnter(Collision other)
    {
        if (useTriggers) return;
        if (other.collider.CompareTag(damageTag))
        {
            TakeDamage(1);
        }
    }
    
    //api
    public void TakeDamage(int amount)
    {
        if  (invulnerable || lives <= 0 ) return;
        
        lives = Mathf.Max(0, lives - Mathf.Abs(amount));
        ApplyAppearance();
        
        if (lives <= 0)
        {
            Die();
            return;
        }
        
        if (invulnerableTime > 0f)
            StartCoroutine(IFrames());
    }
    
    //cambio de materialcito
    void ApplyAppearance()
    {
        if (materialsByLives == null || materialsByLives.Length == 0)
            return;
        
        // índice basado en cuántas vidas ha perdido
        int index = Mathf.Clamp(maxLives - lives, 0, materialsByLives.Length - 1);
        
        if (materialsByLives[index] != null)
        {
            rend.material = materialsByLives[index];
        }
    }
    
    System.Collections.IEnumerator IFrames()
    {
        invulnerable = true;
        yield return new WaitForSeconds(invulnerableTime);
        invulnerable = false;
    }
    
    void Die()
    {
        Debug.Log("ENEMIGO MUERTOOO");
        
        Destroy(gameObject);
    }
}
