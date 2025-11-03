using System;
using UnityEngine;

public class VisionCone : MonoBehaviour
{
    private Senses senses;
    
    //Arritmia Games ayuda aaa
    //1 El objetivo del enemigo
    public GameObject target;
    
    //2 Slider para el tamaño del cono visual en grados, de 0 a 180
    [Range(0, 180)] public int vision;
    
    //3 Distancia máxima hasta donde el enemigo puede ver
    //public int rangoVisual;
    
    //4 indicador visual de cuando el enemigo nos vea
    public GameObject indicadorVisualv;
    public GameObject indicadorVisualr;
    
    //5 variables booleanas que sirven para determinar el estado actual
    [Header("Read Only")] 
    public bool viendoAlObjetivo;
    public bool objetivoDentro;

    private RigidBodySteeringBehaviours steering;
    private bool lastState;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        senses = GetComponent<Senses>();
    }

    void Awake()
    {
        steering = GetComponent<RigidBodySteeringBehaviours>();
        lastState = false;
        if (steering) steering.enabled = true;
    }
    
    void FuncionConoVisual()
    {
        
        //7 calculamos la direccion pero normalizada
        Vector3 puntaMenosCola = Utilities.PuntaMenosCola(target.transform.position, transform.position);
        Vector3 dir = puntaMenosCola.normalized;

        //8 producto punto para saber si está detras o delante
        float prodPunto = Vector3.Dot(transform.forward, dir);

        //9 Cambiamos la expresion del cono visual para que regrese valores entre 0 y 1.
        //float anguloTransformado = (1 - (vision * 0.005555f));
        float cosUmbral = Mathf.Cos((vision * .5f) * Mathf.Deg2Rad);
        
        bool dentroDelCono = prodPunto >= cosUmbral;
        
        //12 verificamos que sea menor que el rango
        float distancia = puntaMenosCola.magnitude;
        
        if (distancia <= senses.radioDeDeteccion)
        {
            objetivoDentro = true;
        }
        else
        {
            objetivoDentro = false; 
        }

        if (objetivoDentro && dentroDelCono)
        {
            viendoAlObjetivo = true;
            if (indicadorVisualr) indicadorVisualr.SetActive(true);
            if (indicadorVisualv) indicadorVisualv.SetActive(false);
        }
        else
        {
            ResetRb();
            viendoAlObjetivo = false;
            if (indicadorVisualr) indicadorVisualr.SetActive(false);
            if (indicadorVisualv) indicadorVisualv.SetActive(true);
        }
        
        if (viendoAlObjetivo)
        {
            // Calcula la dirección hacia el objetivo
            Vector3 direccion = (target.transform.position - transform.position).normalized;
            direccion.y = 0; // opcional: evita que incline hacia arriba/abajo

            // Crea la rotación deseada
            Quaternion rotacionDeseada = Quaternion.LookRotation(direccion);

            // Rota instantáneamente hacia el objetivo
            transform.rotation = rotacionDeseada;
        }
        
    }

    void ResetRb()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //14 aca
        FuncionConoVisual();
        if (viendoAlObjetivo != lastState && steering)
        {
            steering.enabled = viendoAlObjetivo;
            lastState = viendoAlObjetivo;
        }
    }
}
