using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Collider))]
public class SDLBehaviour : MonoBehaviour
{
    public enum Estado { Patrol, Enojo }

    [Header("Objetivo")]
    public Transform target;
    public string playerTag = "Player";

    [Header("Vision (cono)")]
    [Range(0f, 180f)] public float anguloVision = 90f;
    public float radioVision = 12f;
    public LayerMask visionObstacles;
    public bool refreshEnojoMientrasVe = false;

    [Header("Patrol")] 
    [SerializeField] private List<GameObject> waypointTriggers = new List<GameObject>();
    public float waitAtWaypoint = 0.2f;

    [Header("Velocidad")] 
    public float patrolSpeed = 20f;
    public float chaseSpeed = 6f;

    [Header("Enojo")] 
    public float enojoDuration = 3f;

    [Header("Conos visuales")] 
    public GameObject verde;
    public GameObject rojo;

    private Estado _state = Estado.Patrol;
    private NavMeshAgent _agent;
    private float _enojoEndTime;
    private int _wpIndex = 0;
    private float _waitTimer = 0;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.autoTraverseOffMeshLink = true;
        _agent.autoBraking = true;
        _agent.updateRotation = true;
        _agent.speed = patrolSpeed;
        
        Collider col =  GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void Start()
    {
        SetCones(idle: true);
        GoToWaypoint(0);
    }

    void Update()
    {
        switch (_state)
        {
            case Estado.Patrol:
                PatrolTick();
                if (CanSeeTarget()) BeginEnojo();
                break;
            
            case Estado.Enojo:
                if (target != null)
                    _agent.SetDestination(target.position);
                if (refreshEnojoMientrasVe && CanSeeTarget()) 
                    _enojoEndTime = Time.time + enojoDuration;
                if (Time.time >= _enojoEndTime)
                    EndEnojo();
                break;
        }
    }

    void PatrolTick()
    {
        if (waypointTriggers.Count == 0) return;
        if (_agent.pathPending) return;

        if (_agent.remainingDistance <= _agent.stoppingDistance + 0.05f)
        {
            _waitTimer += Time.deltaTime;
            if (_waitTimer >= waitAtWaypoint)
            {
                _waitTimer = 0f;
                GoToNextWaypoint();
            }
        }
    }

    void GoToWaypoint(int index)
    {
        if (index < 0 || index >= waypointTriggers.Count) return;
        _wpIndex = index;
        _agent.SetDestination(waypointTriggers[_wpIndex].transform.position);
    }

    void GoToNextWaypoint()
    {
        if(waypointTriggers.Count == 0) return;
        _wpIndex = (_wpIndex + 1) % waypointTriggers.Count;
        GoToWaypoint(_wpIndex);
    }
    
    //cono de vision
    bool CanSeeTarget()
    {
        if (!target) return false;
        
        Vector3 toTarget = target.position - transform.position;
        float dist = toTarget.magnitude;
        if (dist > radioVision) return false;
        
        Vector3 dir = toTarget.normalized;
        float cosThreshold = Mathf.Cos((anguloVision * 0.5f) * Mathf.Deg2Rad);
        float dot = Vector3.Dot(transform.forward, dir);
        if (dot < cosThreshold) return false;
        
        //linea de vision
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, dir, out var hit, dist))
        {
            if(((1 << hit.collider.gameObject.layer) & visionObstacles) != 0) 
                return false;
        }

        return true;
    }

    void BeginEnojo()
    {
        _state = Estado.Enojo;
        _enojoEndTime = Time.time + enojoDuration;
        _agent.speed = chaseSpeed;
        SetCones(idle: false);
    }

    void EndEnojo()
    {
        _state = Estado.Patrol;
        _agent.speed = patrolSpeed;
        _agent.isStopped = false;
        _agent.ResetPath();
        SetCones(idle: true);
        GoToNextWaypoint();
    }

    void OnTriggerEnter(Collider other)
    {
        //tocar al jugador
        if (other.CompareTag(playerTag))
        {
            Destroy(other.gameObject);
            return;
        }
        
        //cuando entra a un waypoint
        if (waypointTriggers.Contains(other.gameObject))
        {
            GoToNextWaypoint();
        }
    }

    void SetCones(bool idle)
    {
        if (verde) verde.SetActive(idle);
        if (rojo) rojo.SetActive(!idle);
    }
}