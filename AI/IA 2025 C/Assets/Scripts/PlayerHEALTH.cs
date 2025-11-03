using UnityEngine;
using System.Collections;
using System.Collections.Generic;


[RequireComponent(typeof(Renderer))]
public class PlayerHEALTH : MonoBehaviour
{
    [Header("Vidas")]
    public int maxLives = 3;
    public float invulnerableTime = 1f;
    
    [Header("Daño por colisión")]
    [Tooltip("Lista de tags que pueden hacer daño al jugador.")]
    public List<string> damageTags;
    
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
    
    //LA COLISIONE
    void OnCollisionEnter(Collision other)
    { 
        if (IsDamageTag(other.collider.tag))
        {
            TakeDamage(1);
        }
    }
    
    //tag
    bool IsDamageTag(string tagToCheck)
    {
        foreach (string tag in damageTags)
        {
            if (tagToCheck == tag)
                return true;
        }
        return false;
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
        Debug.Log("Jugador sin vidas — Game Over");
        
        LevelManager lm = FindObjectOfType<LevelManager>();
        if (lm != null)
        {
            // delay
            lm.RestartLevel(1.5f);
        }
        
        Destroy(gameObject);
    }
}
