using UnityEngine;

//los : indican herencia. en este caso, HelloWorld hereda de MonoBehaviour.
public class HelloWorld : MonoBehaviour
{
    public Vector3 velocity = Vector3.zero;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //todos los start de todos los MonoBehaviours se van a ejecutar antes que el update de cualquier MonoBehaviour.
    void Start()
    {
        Debug.Log(message:"Hello World");
        Debug.LogWarning(message:"Hello World");
        Debug.LogError(message:"Hello World");
    }

    // Update is called once per frame
    void Update()
    {
        //aceleracion porque multiplicamos por tiempo la posicion y otra vez por tiempo la velocidad
        velocity += new Vector3(0, -9.81f, 0) * Time.deltaTime;
        transform.position +=  velocity * Time.deltaTime;
        
        //idealmente se ejecuta 60 veces por segundo
        //realmente no se ejecuta un numero fijo de veces en el tiempo, se ejecuta todas las que pueda
        //Debug.Log(message:"Hello World");
        
        //quiero que mi objeto se mueva una unidad por frame
        //transform.position += new Vector3(x:1, y:0, z:0);
        
        //quiero que mi objeto se mueva una unidad por segundo
        //transform.position += new Vector3(x:1, y:0, z:0) * Time.deltaTime;
    }

    private void FixedUpdate()
    {
        
    }
}

