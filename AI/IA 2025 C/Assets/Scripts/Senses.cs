using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Este script lo vamos a usar para poner todo lo que tiene que ver con detección, visión, tacto, y oido.
/// </summary>

public class Senses : MonoBehaviour
{
    public static Vector3 PuntaMenosCola(Vector3 punta, Vector3 cola)
    {
        float x = punta.x - cola.x;
        float y = punta.y - cola.y;
        float z = punta.z - cola.z;
        return new Vector3(x, y, z);

        //Internamente, esta línea hace lo que las 4 de arriba hacen.
        //return punta - cola;
    }

    public static float Pitagoras(Vector3 vector3)
    {
        //hipotenusa = sqrt(A^2 + B^2 + C^2)
        float hipotenusa = math.sqrt(vector3.x * vector3.x + 
                                       vector3.y * vector3.y + 
                                       vector3.z * vector3.z);
        return hipotenusa;
        
        //Internamente, esta línea hace lo que las 4 de arriba hacen.
        //return vector3.magnitude;
    }

//vamos a detectar cosas que esten en un radio determinado
    
    
    
    void DetectAllGameObjects()
    {
        
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DetectAllGameObjects();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //OnDrawGizmos se manda a llamar cada que la pestaña de escena se actualiza. Se actualiza incluso cuando no estamos en play mode.
    //OnDrawGizmosSelected hace lo mismo pero solo cuando el GameObject con este script esté seleccionado
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, 10.0f);

        if (Application.isPlaying)
        {
            //esta obtiene todos los gameobjects de la escena
            GameObject[] allGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
            //después los filtramos para que solo nos dé los que sí están dentro del radio determinado
            foreach (var foundGameObject in allGameObjects)
            {
                //Primero hacemos punta menos cola entre la posición de este GameObject y la del foundGameObject
                Vector3 puntaMenosCola = PuntaMenosCola(foundGameObject.transform.position, gameObject.transform.position);
            
                //Luego usamos el teorema de Pitágoras para calcular la Distancia entre este GameObject dueño del script senses y el foundGameObject
                float distancia = Pitagoras(puntaMenosCola);
            
                //Ya con la distancia, vamos a compararla con el radio de visión que determinamos
                if (distancia < 10.0f)
                {
                    Gizmos.color = Color.green;
                    //sí está dentro del radio
                    Gizmos.DrawWireCube(foundGameObject.transform.position, Vector3.one);
                }
                else
                {
                    Gizmos.color = Color.red;
                    //no está dentro del radio
                    Gizmos.DrawWireCube(foundGameObject.transform.position, Vector3.one);
                }
            }
        }
    }
}
