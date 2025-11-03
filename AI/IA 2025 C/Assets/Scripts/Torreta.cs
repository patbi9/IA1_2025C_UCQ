using System;
using UnityEditor.UI;
using UnityEngine;

public class Torreta : BaseEnemy
{
    public enum estadotorreta {Idle, Ataque}

    [Header("Objetivo")] public Transform target;
    [Header("Giro")] public float velocidadGiro = 60f;
    [Header("Vision")]
    [Range(0f, 180f)] public float anguloVision = 90f;
    public float radio = 12f;
    public bool lockToHorizontal = true;
    
    [Header("Conos visuales")]
    public GameObject verde;
    public GameObject rojo;

    [Header("Disparo")] 
    public GameObject dart;
    public Transform firePoint;
    public float dartSpeed = 20f;
    public float fireRate = 6f;
    public float attackDuration = 3f;
    
    private estadotorreta state = estadotorreta.Idle;
    private float lastShotTime;
    private float attackEndTime;
    private Vector3 lockedFireDir;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetCones(idle:true);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        switch (state)
        {
            case estadotorreta.Idle:
                transform.Rotate(0f, velocidadGiro * Time.deltaTime, 0f);
                if (CanSeeTarget())
                    BeginAttack();
                break;
            case estadotorreta.Ataque:
                TryShoot();

                if (Time.time >= attackEndTime)
                    EndAttack();
                break;
        }
    }
    
    //vision
    bool CanSeeTarget()
    {
        if (!target) return false;

        Vector3 toTarget = target.position - transform.position;
        float dist = toTarget.magnitude;
        if (dist > radio) return false;

        if (lockToHorizontal) toTarget.y = 0f;
        if(toTarget.sqrMagnitude < 0.00001f) return false;
        
        Vector3 dir = toTarget.normalized;
        float cosThreshold = Mathf.Cos((anguloVision / 2f) * Mathf.Deg2Rad);
        float dot = Vector3.Dot(transform.forward, dir);
        return dot >= cosThreshold;
    }
    
    //estados
    void BeginAttack()
    {
        state = estadotorreta.Ataque;
        
        //congelar direccion
        lockedFireDir = (target ? target.position - transform.position : transform.forward);
        if (lockToHorizontal) lockedFireDir.y = 0f;
        if (lockedFireDir.sqrMagnitude < 0.00001f) lockedFireDir = transform.forward;
        lockedFireDir.Normalize();

        attackEndTime = Time.time + attackDuration;
        lastShotTime = -999f;

        SetCones(idle: false); //rojo si, verde no
    }

    void EndAttack()
    {
        state = estadotorreta.Idle;
        SetCones(idle: true); //verde si, rojo no
    }
    
    //disparar
    void TryShoot()
    {
        if (!dart || !firePoint) return;
        
        float interval = 1f /Mathf.Max(0.01f, fireRate);
        if(Time.time - lastShotTime < interval) return;
        
        lastShotTime = Time.time;
        
        GameObject dardo = Instantiate(dart, firePoint.position + firePoint.forward * 2.0f, firePoint.rotation);
        dardo.transform.Rotate(0f, 0f, 0f);
        Rigidbody rbDardo = dart.GetComponent<Rigidbody>();

        if (dart != null)
        {
            rbDardo.linearVelocity = firePoint.forward * dartSpeed;
        }
        
    }
    
    //conos
    void SetCones(bool idle)
    {
        if (verde) verde.SetActive(idle);
        if (rojo) rojo.SetActive(!idle);
    }
    
}
