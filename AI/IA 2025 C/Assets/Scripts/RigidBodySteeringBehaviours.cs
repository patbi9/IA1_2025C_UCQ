using System;
using UnityEngine;

public class RigidBodySteeringBehaviours : MonoBehaviour
{
    
    // Velocidad máxima a la que puede ir este agente.
    public float maxSpeed = 10.0f;
    
    // máxima fuerza que se le puede aplicar
    public float maxForce = 5.0f;

    public float lookAheadTime = 1.0f;
    
    // Componente que maneja las fuerzas y la velocidad de nuestro agente.
    protected Rigidbody _rb;
    
    // Posición del objetivo.
    public Vector3 _targetPosition = Vector3.zero;
    public Rigidbody _targetRb; // Rigidbody del objetivo.


    protected bool _targetIsSet = false;

    public void SetTarget(Vector3 target, Rigidbody targetRb)
    {
        _targetPosition = target;
        _targetRb = targetRb;
        _targetIsSet = true;
    }

    public void RemoveTarget()
    {
        _targetIsSet = false;
        _targetRb = null; // lo quitamos, ahorita por pura seguridad, pero idealmente hay que quitarlo.
    }

    public void Start()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
        {
            Debug.LogWarning($"No se encontró el rigidbody para el agente: {name}. ¿Sí está asignado?");
        }
    }

    public Vector3 Seek(Vector3 targetPosition)
    {
        // Si sí hay un objetivo, empezamos a hacer Seek, o sea, a perseguir ese objetivo.
        // Lo primero es obtener la dirección deseada. El método punta menos cola lo usamos con nuestra posición
        // como la cola, y la posición objetivo como la punta
        Vector3 puntaMenosCola = Senses.PuntaMenosCola(targetPosition, transform.position);
        Vector3 desiredDirection = puntaMenosCola.normalized; // normalized nos da la pura dirección con una magnitud de 1.

        // Ya que tenemos esa dirección, la multiplicamos por nuestra velocidad máxima posible, y eso es la velocidad deseada.
        Vector3 desiredVelocity = desiredDirection * maxSpeed;
        
        // La steering force es la diferencia entre la velocidad deseada y la velocidad actual
        Vector3 steeringForce = desiredVelocity - _rb.linearVelocity;
        return steeringForce;
    }
    
    // Update is called a fixed number of times each second. 50 by default.
    void FixedUpdate()
    {
        // Ver si hay una posición objetivo a la cual moverse.
        if (!_targetIsSet)
            return; // si no lo hay, no hagas nada.
        
        // Pursuit
        // Tenemos que obtener la posición futura del objetivo. Necesitamos:
        // A) La posición actual del objetivo.
        // B) la velocidad actual del objetivo (el vector que trae tanto magnitud como dirección)
        // C) el tiempo en el futuro en el que queremos predecir (por ejemplo, 2 segundos, 5 segundos, 1 hora, etc.)
        // _targetPosition
        Vector3 targetCurrentVelocity = _targetRb.linearVelocity;
        
        Vector3 predictedPosition = _targetPosition + targetCurrentVelocity * lookAheadTime;

        
        Vector3 puntaMenosCola = Senses.PuntaMenosCola(predictedPosition, transform.position);
        Vector3 desiredDirection = puntaMenosCola.normalized; // normalized nos da la pura dirección con una magnitud de 1.

        // Ya que tenemos esa dirección, la multiplicamos por nuestra velocidad máxima posible, y eso es la velocidad deseada.
        Vector3 desiredVelocity = desiredDirection * maxSpeed;
        
        // La steering force es la diferencia entre la velocidad deseada y la velocidad actual
        Vector3 steeringForce = desiredVelocity - _rb.linearVelocity;

        
        // la steering force no puede ser mayor que la max steering force PERO sí puede ser menor.
        steeringForce = Vector3.ClampMagnitude(steeringForce, maxForce);
        

        // Aplicamos esta fuerza para mover a nuestro agente.
        _rb.AddForce(steeringForce, ForceMode.Acceleration);

        if (_rb.linearVelocity.magnitude > maxSpeed)
        {
            Debug.Log($"Cuidado, _currentVelocity es mayor que maxSpeed: {_rb.linearVelocity.magnitude}");
        }
        
        // el cambio de posición ya lo hace automáticamente el rigidbody por nosotros.
        

    }

    protected void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            return;
        
        Gizmos.color = Color.red;
        
        Gizmos.DrawCube(_targetPosition, Vector3.one*0.5f);

        // Si sí hay un rigidbody del target para hacerle Pursuit of evade:
        if (_targetRb != null)
        {
            // dibujamos el gizmo de la posición predicha.
            Vector3 targetCurrentVelocity = _targetRb.linearVelocity;
            Vector3 predictedPosition = _targetPosition + targetCurrentVelocity * lookAheadTime;
            Gizmos.color = Color.yellow;
        
            Gizmos.DrawCube(predictedPosition, Vector3.one*0.5f);   
            
            // Línea desde el agente hasta la posición predicha:
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, predictedPosition);
            
            // línea hacia la posición predicha, pero con la magnitud de nuestra maxSpeed
            Vector3 directionToPredictedPosition = (predictedPosition - transform.position).normalized;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + directionToPredictedPosition*maxSpeed);
            
            // línea de la velocidad real a la que va este agente
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + _rb.linearVelocity);

            
            // Flecha de la steering force
            Vector3 puntaMenosCola = Senses.PuntaMenosCola(predictedPosition, transform.position);
            Vector3 desiredDirection = puntaMenosCola.normalized; // normalized nos da la pura dirección con una magnitud de 1.

            // Ya que tenemos esa dirección, la multiplicamos por nuestra velocidad máxima posible, y eso es la velocidad deseada.
            Vector3 desiredVelocity = desiredDirection * maxSpeed;
        
            // La steering force es la diferencia entre la velocidad deseada y la velocidad actual
            Vector3 steeringForce = desiredVelocity - _rb.linearVelocity;

        
            // la steering force no puede ser mayor que la max steering force PERO sí puede ser menor.
            steeringForce = Vector3.ClampMagnitude(steeringForce, maxForce);
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, transform.position + steeringForce);
        }

    }
}