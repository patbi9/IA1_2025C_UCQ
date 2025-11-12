using System;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.XR;

public class EscapistEnemy : BaseEnemy
{
    public enum State
    {
        Active,
        Tired
    }

    [Header("Refs")] [SerializeField] private Transform player;
    [SerializeField] private GameObject tiredVFX;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private LayerMask visionObstacles;

    [Header("Movement")] [SerializeField] private NavMeshAgent agent;
    [SerializeField] private float maxSpeed = 3.0f;
    [SerializeField] private float acceleration = 80.0f;

    [Header("Ranges")] [SerializeField] private float detectionRadius = 10.0f;
    [SerializeField] private float shootRadius = 20.0f;

    [Header("Escape")] [SerializeField] private float escapeDistance = 10.0f;
    [SerializeField] private float arriveTolerance = 0.8f;

    [Header("Timers")] [SerializeField] private float activeToTired = 3.0f;
    [SerializeField] private float tiredDuration = 5.0f;

    [Header("Shoot")] [SerializeField] private float activeFireRate = 4.0f;
    [SerializeField] private float tiredFireRate = 1.5f;
    [SerializeField] private float projectileSpeed = 22.0f;

    private State actualState = State.Active;
    private float lastShotTime = -999f;
    private bool escapeProgrammed = false;
    private Vector3 escapePos = Vector3.zero;
    private bool isEscaping = false;

    private Coroutine toTiredCoroutine = null;
    private Coroutine tiredCoroutine = null;

    private void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Awake()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        agent.speed = maxSpeed;
        agent.acceleration = acceleration;
        agent.angularSpeed = 720f;
        agent.autoBraking = true;

        if (tiredVFX != null) tiredVFX.SetActive(false);
    }

    private void OnEnable()
    {
        ChangeState(State.Active);
    }

    private void Update()
    {
        if (player == null) return;

        switch (actualState)
        {
            case State.Active:
                TickActive();
                break;
            case State.Tired:
                TickTired();
                break;
        }

        if (IsInShootRange() && CanSeePlayer())
        {
            float rate = actualState == State.Tired ? tiredFireRate : activeFireRate;
            float delay = 1f / Math.Max(0.1f, rate);
            if (Time.time - lastShotTime >= delay)
            {
                Debug.Log("dispari");
                Shoot();
                lastShotTime = Time.time;
            }
        }
    }

    //activ
    private void TickActive()
    {
        float dist = DistanceToPlayer();

        if (!isEscaping)
        {
            if (dist > detectionRadius)
            {
                //constantemente ir al jugadore
                agent.isStopped = false;
                agent.speed = maxSpeed;
                agent.acceleration = acceleration;
                agent.SetDestination(player.position);
                escapeProgrammed = false;
            }
            else
            {
                StartEscape();
            }
        }
        else
        {
            if (!agent.pathPending && agent.remainingDistance <= arriveTolerance)
            {
                agent.isStopped = true;
            }
        }
    }

    private void StartEscape()
    {
        isEscaping = true;
        
        //punto opuesto a 11m
        Vector3 opDirection = (transform.position - player.position);
        opDirection.y = 0f;
        if(opDirection.sqrMagnitude < 0.0001f) opDirection = transform.right;
        opDirection.Normalize();
        Vector3 candidate = transform.position + opDirection * escapeDistance;

        if (IsValidNVPoint(candidate, out Vector3 valid))
        {
            escapePos = valid;
            agent.isStopped = false;
            agent.SetDestination(escapePos);
        }
        else
        {
            Vector3 dirToPlayer = (player.position - transform.position);
            dirToPlayer.y = 0f;
            if (dirToPlayer.sqrMagnitude < 0.0001f) dirToPlayer = transform.forward;
            dirToPlayer.Normalize();
            Vector3 tp = transform.position + dirToPlayer * escapeDistance;
            if (NavMesh.SamplePosition(tp, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                escapePos = hit.position;
            }
            else
            {
                agent.Warp(tp);
                escapePos = tp;
            }
        }

        if (!escapeProgrammed)
        {
            SafeStop(ref toTiredCoroutine);
            toTiredCoroutine = StartCoroutine(ProgramToTiredChange());
            escapeProgrammed = true;
        }
    }

    private IEnumerator ProgramToTiredChange()
    {
        yield return new WaitForSeconds(activeToTired);
        ChangeState(State.Tired);
    }

    private void TickTired()
    {
        agent.isStopped = true;
        agent.ResetPath();
    }

    private IEnumerator TimerTired()
    {
        yield return new WaitForSeconds(tiredDuration);
        ChangeState(State.Active);
    }

    private void ChangeState(State newState)
    {
        actualState = newState;

        if (actualState == State.Active)
        {
            Debug.Log("Active state");
            if (tiredVFX) tiredVFX.SetActive(false);
            agent.speed = maxSpeed;
            agent.acceleration = acceleration;
            agent.isStopped = false;
            SafeStop(ref tiredCoroutine);
            escapeProgrammed = false;
            isEscaping = false;
        }
        else
        {
            Debug.Log("Tired state");
            if (tiredVFX) tiredVFX.SetActive(true);
            agent.isStopped = true;
            agent.ResetPath();
            SafeStop(ref toTiredCoroutine);
            SafeStop(ref tiredCoroutine);
            tiredCoroutine = StartCoroutine(TimerTired());
            isEscaping = false;
        }
    }
    
    private void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;

        GameObject go = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        
        if (go.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            Vector3 dir = (player.position + Vector3.up * 0.5f - firePoint.position).normalized;
            rb.linearVelocity = dir * projectileSpeed;
        }
        else
        { 
            go.AddComponent<Rigidbody>().useGravity = false;
            go.GetComponent<Rigidbody>().linearVelocity =
                (player.position + Vector3.up * 0.5f - firePoint.position).normalized * projectileSpeed;
        }
    }

    private bool IsInShootRange()
    {
        return Vector3.Distance(transform.position, player.position) <= shootRadius;
    }

    private float DistanceToPlayer()
    {
        return Vector3.Distance(transform.position, player.position);
    }

    private bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 origin = transform.position + Vector3.up * 1.2f;
        Vector3 destination = player.position + Vector3.up * 1.2f;
        Vector3 direction = destination - origin;
        float distance = direction.magnitude;
        if (distance < 0.1f) return true;

        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, visionObstacles,
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }
        return true;
    }

    private bool IsValidNVPoint(Vector3 p, out Vector3 valid)
    {
        if (NavMesh.SamplePosition(p, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
        {
            valid = hit.position;
            return true;
        }

        valid = p;
        return false;
    }
    
    private void SafeStop(ref Coroutine co)
    {
        if (co != null)
        {
            try { StopCoroutine(co); } catch {}   
            co = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.darkGreen;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shootRadius);

        if (escapePos != Vector3.zero)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(escapePos, 0.25f);
            Gizmos.DrawLine(transform.position, escapePos);
        }
    }
}
    
