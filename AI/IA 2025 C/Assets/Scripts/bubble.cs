using UnityEngine;

public class bubble : MonoBehaviour
{
    public float speed = 1f;
    public float lifetime = 10f;
    
    
    void OnCollisionEnter(Collision other)
    {
        // choca con un enemigo y desaparece
        if (other.collider.CompareTag("Enemy"))
        {
            Destroy(gameObject); // destruye la burbuja
        }
    }

    void Start() => Destroy(gameObject, lifetime);

    void FixedUpdate()
    {
        transform.position += transform.forward * (speed * Time.fixedDeltaTime);
    }
}
