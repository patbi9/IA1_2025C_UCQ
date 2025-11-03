using UnityEngine;
using UnityEngine.PlayerLoop;

public class WanderSteeringBehaviour : RigidBodySteeringBehaviours
{
   
   public float maxWanderDistance = 10.0f;
   public float minWanderDistance = 3.0f;

   public float radiusToTargetPositionTolerance = 1.0f;
   private Vector3 _currentTargetPosition;


   public void UpdateWanderTargetPosition()
   {
      //el random
      Vector3 randomDirection = new Vector3(Random.Range(-1.0f,1.0f), 0.0f, Random.Range(-1.0f,1.0f)).normalized;
      
      float randomDistance = Random.Range(minWanderDistance, maxWanderDistance);
      
      _currentTargetPosition = transform.position + randomDirection * randomDistance;
   }

   public void Start()
   {
      base.Start();
      
      _currentTargetPosition = transform.position; //se inicializa
   }
   
   
   public void FixedUpdate()
   {
      Vector3 steeringForce = Vector3.zero;
      //el wander se hace constantemente
      
      //si ya estamos cerca de la target pos, obtenemos una nueva

      if (Utilities.IsObjectInRange(transform.position, _currentTargetPosition, radiusToTargetPositionTolerance))
      {
         UpdateWanderTargetPosition();
      }
      
      steeringForce = Arrive(_currentTargetPosition);
      
      steeringForce += ObstacleAvoidance();
      
      
      // la steering force no puede ser mayor que la max steering force PERO sí puede ser menor.
      steeringForce = Vector3.ClampMagnitude(steeringForce, maxForce);
        

      // Aplicamos esta fuerza para mover a nuestro agente.
      _rb.AddForce(steeringForce, ForceMode.Acceleration);

      if (_rb.linearVelocity.magnitude > maxSpeed)
      {
         Debug.Log($"Cuidado, _currentVelocity es mayor que maxSpeed: {_rb.linearVelocity.magnitude}");
      }
   }

   void OnDrawGizmosSelected()
   {
      if (!Application.isPlaying)
         return;
      
      Gizmos.color = Color.crimson;
      Gizmos.DrawWireSphere(_currentTargetPosition, 0.5f);
   }
   
}
