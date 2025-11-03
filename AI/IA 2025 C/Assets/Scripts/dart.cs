using UnityEngine;

public class dart : MonoBehaviour
{
    public float speed = 1f;
    public float lifeTime = 10f;

    void OnCollisionEnter(Collision other)
    {
            Destroy(gameObject); //destruye el dardo
    }
    
    void Start() => Destroy(gameObject, lifeTime);

    void FixedUpdate()
    {
        transform.position += transform.forward * (speed * Time.fixedDeltaTime);
    }
}
