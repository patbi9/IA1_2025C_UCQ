using System;
using System.Collections.Generic;
using System.Security;
using UnityEngine;

public class RigidBodySteeringBehaviours : MonoBehaviour
{

    public ESteeringBehaviours currentBehaviour = ESteeringBehaviours.Seek;
    
    // Velocidad máxima a la que puede ir este agente.
    public float maxSpeed = 10.0f;
    
    // máxima fuerza que se le puede aplicar
    public float maxForce = 5.0f;

    //public float lookAheadTime = 1.0f;

    //radio para comenzar a frenar
    public float arriveBrakeRadius = 5.0f;

    public float arriveToleranceRadius = 0.5f;
    public float arriveSpeedTolerance = 0.5f;
    
    //poder activar o desactivar el obstacle avoidance
    public bool useObstacleAvoidance = true;
    public float obstacleAvoidanceDetectionRadius = 5.0f;
    public LayerMask obstacleAvoidanceLayerMask;
    
    
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
    
    public Vector3 PredictPosition(Vector3 startingTargetPosition, Vector3 targetVelocity)
    {
        //distancia entre el objetivo y yo / máxima velocidad
        float lookAheadCalculado = Utilities.PuntaMenosCola(startingTargetPosition, transform.position).magnitude/maxSpeed;
        
        // Pursuit
        // Tenemos que obtener la posición futura del objetivo. Necesitamos:
        // A) La posición actual del objetivo.
        // B) la velocidad actual del objetivo (el vector que trae tanto magnitud como dirección)
        // C) el tiempo en el futuro en el que queremos predecir (por ejemplo, 2 segundos, 5 segundos, 1 hora, etc.)
        // _targetPosition
        Vector3 targetCurrentVelocity = targetVelocity;
        
        Vector3 predictedPosition = startingTargetPosition + targetCurrentVelocity * lookAheadCalculado;
        return predictedPosition;
    }
    
    public Vector3 Seek(Vector3 targetPosition)
    {
        // Si sí hay un objetivo, empezamos a hacer Seek, o sea, a perseguir ese objetivo.
        // Lo primero es obtener la dirección deseada. El método punta menos cola lo usamos con nuestra posición
        // como la cola, y la posición objetivo como la punta
        Vector3 puntaMenosCola = Utilities.PuntaMenosCola(targetPosition, transform.position);
        Vector3 desiredDirection = puntaMenosCola.normalized; // normalized nos da la pura dirección con una magnitud de 1.

        // Ya que tenemos esa dirección, la multiplicamos por nuestra velocidad máxima posible, y eso es la velocidad deseada.
        Vector3 desiredVelocity = desiredDirection * maxSpeed;
        
        // La steering force es la diferencia entre la velocidad deseada y la velocidad actual
        Vector3 steeringForce = desiredVelocity - _rb.linearVelocity;
        return steeringForce;
    }

    //flee
    public Vector3 Flee(Vector3 targetPosition)
    {
        return -Utilities.Seek(targetPosition, transform.position, maxSpeed, _rb.linearVelocity);
        //lo contrario de seek
        //return -Seek(targetPosition);
    }

    public Vector3 Pursuit(Vector3 targetPosition)
    {
        
        Vector3 predictedPosition = Utilities.PredictPosition(transform.position, _targetPosition, _targetRb.linearVelocity, maxSpeed);
        //Vector3 predictedPosition = PredictPosition(_targetPosition, _targetRb.linearVelocity);

        Vector3 steeringForce = Utilities.Seek(targetPosition, transform.position, maxSpeed, _rb.linearVelocity);
        return steeringForce;
    }

    public Vector3 Evade(Vector3 targetPosition)
    {
        Vector3 steeringForce = -Pursuit(targetPosition);
        return steeringForce;
    }

    public Vector3 Arrive(Vector3 targetPosition)
    {
        Vector3 steeringForce = Vector3.zero;
        
        //si no está en el rango, haz seek
        if (!Utilities.IsObjectInRange(transform.position, targetPosition, arriveBrakeRadius))
        {
            return Seek(targetPosition);
        }
        
        Vector3 arrowToTarget = targetPosition - transform.position;
        
        //distancia
        float distanceToTarget = arrowToTarget.magnitude;
        //si ya llegamos, nos detenemos
        if (distanceToTarget <= arriveToleranceRadius && _rb.linearVelocity.magnitude <= arriveSpeedTolerance)
        {
            _rb.linearVelocity =  Vector3.zero;
            return Vector3.zero;
        }
            
            
        Vector3 directionToTarget = arrowToTarget.normalized;
        
        //si sí está dentro del rango, hay que empezar a frenar con lerp
        float desiredSpeed = Mathf.Lerp(0.0f, maxSpeed, distanceToTarget / arriveBrakeRadius);
        Vector3 desiredVelocity = directionToTarget * desiredSpeed;
        
        steeringForce = desiredVelocity - _rb.linearVelocity;
        return steeringForce;
    }

    public Vector3 ObstacleAvoidance()
    {
        Vector3 steeringForce = Vector3.zero;
        if (useObstacleAvoidance)
        {
            //vamos a sumar las fuerzas de los obstáculos
            var obstacles= Utilities.GetObjectsInRadius(transform.position, obstacleAvoidanceDetectionRadius,
                obstacleAvoidanceLayerMask);
            //hay que checar que tanta fuerza aplica cada uno de ellos
            foreach (var obstacle in obstacles)
            {
                //[puedes explicarme que hacen estas dos lineas porfa?]
                float distanceToObstacle = (transform.position - obstacle.transform.position).magnitude;
                steeringForce += Flee(obstacle.transform.position) * distanceToObstacle / obstacleAvoidanceDetectionRadius;
            }
            
        }

        return steeringForce;
    }
    
    
    // Update is called a fixed number of times each second. 50 by default.
    void FixedUpdate()
    {
        // Ver si hay una posición objetivo a la cual moverse.
        if (!_targetIsSet)
            return; // si no lo hay, no hagas nada.
        
        Vector3 steeringForce = Vector3.zero;
        switch (currentBehaviour)
        {
            case ESteeringBehaviours.DontMove:
                _rb.linearVelocity = Vector3.zero;
                break;
            case ESteeringBehaviours.Seek:
                steeringForce = Seek(_targetPosition);
                break;
            case ESteeringBehaviours.Flee:
                steeringForce = Flee(_targetPosition);
                break;
            case ESteeringBehaviours.Pursuit:
                steeringForce = Pursuit(_targetPosition);
                break;
            case ESteeringBehaviours.Evade:
                steeringForce = Evade(_targetPosition);
                break;
            case ESteeringBehaviours.Arrive:
                steeringForce = Arrive(_targetPosition);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        steeringForce += ObstacleAvoidance();
        
        
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
            Vector3 targetPosition = _targetPosition; //Por defecto
            
            Vector3 steeringForce = Vector3.zero;
            switch (currentBehaviour)
            {
                case ESteeringBehaviours.DontMove:
                    //nada baby
                    break;
                case ESteeringBehaviours.Seek:
                    steeringForce = Seek(_targetPosition);
                    break;
                case ESteeringBehaviours.Flee:
                    steeringForce = Flee(_targetPosition);
                    break;
                case ESteeringBehaviours.Pursuit:
                {
                    //el gizmo de la posición predicha
                    Vector3 predictedPosition = PredictPosition(_targetPosition, _targetRb.linearVelocity);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawCube(predictedPosition, Vector3.one * 0.5f);
                    targetPosition = predictedPosition;
                    steeringForce = Pursuit(_targetPosition);
                }
                    break;
                case ESteeringBehaviours.Evade:
                {
                    //el gizmo de la posición predicha
                    Vector3 predictedPosition = PredictPosition(_targetPosition, _targetRb.linearVelocity);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawCube(predictedPosition, Vector3.one * 0.5f);
                    targetPosition = predictedPosition;
                    steeringForce = Evade(_targetPosition);
                }
                    break;
                case ESteeringBehaviours.Arrive:
                    steeringForce = Arrive(_targetPosition);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            
            
            
            
           
            
            // Línea desde el agente hasta la posición predicha:
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetPosition);
            
            // línea hacia la posición objetivo, pero con la magnitud de nuestra maxSpeed
            Vector3 directionToPosition = (targetPosition - transform.position).normalized;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + directionToPosition*maxSpeed);
            
            // línea de la velocidad real a la que va este agente
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + _rb.linearVelocity);
        
            //steering force flecha

        
            // la steering force no puede ser mayor que la max steering force PERO sí puede ser menor.
            steeringForce = Vector3.ClampMagnitude(steeringForce, maxForce);
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, transform.position + steeringForce);
        }

        if (useObstacleAvoidance)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, obstacleAvoidanceDetectionRadius);
        }

    }
}