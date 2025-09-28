using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
//using UnityEditor.Experimental.GraphView;
using UnityEngine;

/// <summary>
/// Lo vamos a usar para poner todo lo que tiene que ver con detección, visión, y si da tiempo, tacto y oído.
/// </summary>
public class Senses : MonoBehaviour
{

    // Opciones para remplazar valores hardcodeados:
    // 1) hacer una variable que se puede cambiar desde el editor.
    public float radioDeDeteccion = 20.0f;
    // 1.A) hacer una variable no-pública que se puede cambiar desde el editor
    [SerializeField] private float radioDeDeteccionPrivado = 2.0f;

    [SerializeField] private LayerMask desiredDetectionLayers; 
    
    // 2) variable pública estática.
    // La variable static solo una vez se le puede asignar valor y después ya nunca puede cambiar.
    public static float RadioDeDeteccionStatic = 20.0f;
    
    // 2.A) variable const
    // es muy parecida a la static, pero NO se le puede asignar un valor ya en la ejecución.
    public const float RADIO_DE_DETECCION_CONST = 20.0f;
    
    // 3) Scriptable Objects
    // es un tipo de clase especial que sirve principalmente para guardar datos, pero también puede tener funciones.
    // Solo se instancía una vez, y todos los que referencíen a ese scriptableObject pueden acceder a esa única instancia.
    // Ayuda muchísimo a reducir el uso de memoria cuando A) se va a remplazar muchos datos de una clase y
    // B) cuando va a haber muchos que usen esos datos
    
    // 4) Un archivo de configuración.
    
    // List en C# es el equivalente de vector<> en C++ (es decir, es un array de tamaño dinámico, no una lista ligada).
    // Lista de GameObjects encontrados este frame
    private List<GameObject> _foundGameObjects ;
    public List<GameObject> foundGameObjects => _foundGameObjects;

    
    public static Vector3 PuntaMenosCola(Vector3 punta, Vector3 cola)
    {
        float x = punta.x - cola.x;
        float y = punta.y - cola.y;
        float z = punta.z - cola.z;
        return new Vector3(x, y, z);
        
        // Internamente, esta línea hace lo que las 4 líneas de arriba harían.
        // return punta - cola;
    }

    public static float Pitagoras(Vector3 vector3)
    {
        // hipotenusa = raíz cuadrada de a^2 + b^2 + c^2
        float hipotenusa = math.sqrt(vector3.x * vector3.x +
                                     vector3.y * vector3.y +
                                     vector3.z * vector3.z);
        return hipotenusa;

        // return vector3.magnitude;
    }
        
    
    // Vamos a detectar cosas que estén en un radio determinado.
    void DetectarTodosLosGameObjects()
    {
        // Esta obtiene TODOS los gameObjects en la escena.
        _foundGameObjects = GetGameObjectsInsideRadius(radioDeDeteccion, transform.position);
    }

    public static List<GameObject> GetGameObjectsInsideRadius(float radius, Vector3 position)
    {
        List<GameObject> foundGO = FindObjectsByType<GameObject>(FindObjectsSortMode.InstanceID).ToList();

        List<GameObject> gameObjectsInsideRadius = new List<GameObject>();
        
        // Después los filtramos para que solo nos dé los que sí están dentro del radio determinado.
        foreach (var foundGameObject in foundGO)
        {
            if (IsObjectInRange(foundGameObject.transform.position, position, radius))
            {
                gameObjectsInsideRadius.Add(foundGameObject);
            }
        }

        return gameObjectsInsideRadius;
    }

    /// <summary>
    /// Requiere que los objetos a detectarse tengan colliders que toquen a la esfera descrita por estos parámetros.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="radius"></param>
    /// <param name="desiredLayers"></param>
    /// <returns></returns>
    public static List<GameObject> GetObjectsInRadius(Vector3 position, float radius, LayerMask desiredLayers)
    {
        Collider[] collidersInRadius = Physics.OverlapSphere(position, radius, desiredLayers);

        List<GameObject> objectsInRadius = new List<GameObject>();
        foreach (var collider in collidersInRadius)
        {
            objectsInRadius.Add(collider.GameObject());
        }

        return objectsInRadius;
    }

    public static List<GameObject> GetObjectsInCube(Vector3 position, Vector3 extents, Quaternion orientation, LayerMask desiredLayers)
    {
        Collider[] collidersInBox = Physics.OverlapBox(position, extents, Quaternion.identity, desiredLayers);

        List<GameObject> objectsInBox = new List<GameObject>();
        foreach (var collider in collidersInBox)
        {
            objectsInBox.Add(collider.GameObject());
        }

        return objectsInBox;
    }
    
    public static bool IsObjectInRange(Vector3 posA, Vector3 posB, float range)
    {
        // Primero hacemos punta menos cola entre la posición de este GameObject y la del foundGameObject,
        // esto nos da la flecha que va del uno al otro,
        Vector3 puntaMenosCola = PuntaMenosCola(posA, posB);

        // Y ya con esa flecha, usamos el teorema de Pitágoras, para calcular la distancia entre este gameObject
        // que es dueño de este script Senses y el foundGameObject.
        float distancia = Pitagoras(puntaMenosCola);

        // ya con la distancia calculada, la comparamos contra este radio que determinamos.
        if (distancia < range)
        {
            // Sí está dentro del radio
            return true;
        }

        // no está dentro del radio.
        return false;
    }
    
    public List<GameObject> GetAllObjectsByLayer(int layer)
    {
        List<GameObject> objects = new List<GameObject>();
        foreach (var foundObject in _foundGameObjects)
        {
            // break; // break es: salte del ciclo donde estés.

            if (foundObject.layer != layer)
                continue; // continue es: vete a la siguiente iteración del ciclo en donde estás.
                
            if (IsObjectInRange(foundObject.transform.position, transform.position, radioDeDeteccion))
            {
                objects.Add(foundObject);
            }
                
        }

        return objects;
    }

    public List<GameObject> GetPlayers()
    {
        return GetAllObjectsByLayer(LayerMask.NameToLayer("Player"));
    }
    
    public List<GameObject> GetEnemies()
    {
        return GetAllObjectsByLayer(LayerMask.NameToLayer("Enemy"));
    }
    
    public List<GameObject> GetBullets()
    {
        return GetAllObjectsByLayer(LayerMask.NameToLayer("Bullet"));
    }

    // esta es la que se usa para comparar entre Tags
    public static bool CompareString(string a, string b)
    {
        // si no son de la misma longitud, no pueden ser iguales
        if (a.Length != b.Length)
            return false;

        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
                return false;
        }
        
        return true;
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //DetectarTodosLosGameObjects(); //esta ya no pq es mas pesada que la de abajo
        _foundGameObjects = GetObjectsInRadius(transform.position, radioDeDeteccion, desiredDetectionLayers);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, radioDeDeteccion);

        if (Application.isPlaying)
        {
            // Después los filtramos para que solo nos dé los que sí están dentro del radio determinado.
            foreach (var foundGameObject in _foundGameObjects)
            {
                if (IsObjectInRange(foundGameObject.transform.position, transform.position, radioDeDeteccion))
                {
                    Gizmos.color = Color.green;
                    // sí está dentro del radio
                    Gizmos.DrawWireCube(foundGameObject.transform.position, Vector3.one);
                }
                else
                {
                    Gizmos.color = Color.red;
                    // no está dentro del radio.
                    Gizmos.DrawWireCube(foundGameObject.transform.position, Vector3.one);
                }
                
            }
        }
    }
}