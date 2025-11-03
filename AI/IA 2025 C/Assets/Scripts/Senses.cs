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
    //[SerializeField] private float radioDeDeteccionPrivado = 2.0f;

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
            if (Utilities.IsObjectInRange(foundGameObject.transform.position, position, radius))
            {
                gameObjectsInsideRadius.Add(foundGameObject);
            }
        }

        return gameObjectsInsideRadius;
    }
    
    public List<GameObject> GetAllObjectsByLayer(int layer)
    {
        List<GameObject> objects = new List<GameObject>();
        foreach (var foundObject in _foundGameObjects)
        {
            // break; // break es: salte del ciclo donde estés.

            if (foundObject.layer != layer)
                continue; // continue es: vete a la siguiente iteración del ciclo en donde estás.
                
            if (Utilities.IsObjectInRange(foundObject.transform.position, transform.position, radioDeDeteccion))
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
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //DetectarTodosLosGameObjects(); //esta ya no pq es mas pesada que la de abajo
        _foundGameObjects = Utilities.GetObjectsInRadius(transform.position, radioDeDeteccion, desiredDetectionLayers);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, radioDeDeteccion);

        if (Application.isPlaying)
        {
            // Después los filtramos para que solo nos dé los que sí están dentro del radio determinado.
            foreach (var foundGameObject in _foundGameObjects)
            {
                if (Utilities.IsObjectInRange(foundGameObject.transform.position, transform.position, radioDeDeteccion))
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