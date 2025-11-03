using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    [Header("Movimiento")]
    public float velocidadMovimiento = 5f;
    public float velocidadRotacion = 200f;

    [Header("Disparo")]
    public GameObject bulletPrefab;
    public Transform firePoint; // salida de la bala
    public float bulletSpeed = 10f;

    private Rigidbody rb;

    // Acciones del nuevo Input System
    private InputAction moveAction;
    private InputAction rotateAction;
    private InputAction fireAction;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        // Movimiento WASD
        moveAction = new InputAction("Move", InputActionType.Value, "<Gamepad>/leftStick");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/s").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/a").With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/d").With("Right", "<Keyboard>/rightArrow");

        // Rotación A/D
        rotateAction = new InputAction("Rotate", InputActionType.Value); 
        rotateAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a").With("Negative", "<Keyboard>/leftArrow")
            .With("Positive", "<Keyboard>/d").With("Positive", "<Keyboard>/rightArrow");
        rotateAction.AddBinding("<Gamepad>/leftStick/x");

        // disparo con espacio
        fireAction = new InputAction("Fire", InputActionType.Button);
        fireAction.AddBinding("<Keyboard>/space");
        fireAction.performed += ctx => Disparar();
    }

    void OnEnable()
    {
        moveAction.Enable();
        rotateAction.Enable();
        fireAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        rotateAction.Disable();
        fireAction.Disable();
    }

    void FixedUpdate()
    {
        Vector2 move = moveAction.ReadValue<Vector2>();
        float rot = rotateAction.ReadValue<float>();

        Vector3 desplazamiento = transform.forward * (move.y * velocidadMovimiento * Time.fixedDeltaTime);
        rb.MovePosition(rb.position + desplazamiento);

        float grados = rot * velocidadRotacion * Time.fixedDeltaTime;
        Quaternion rotacionDeseada = Quaternion.Euler(0f, grados, 0f);
        rb.MoveRotation(rb.rotation * rotacionDeseada);
    }
    
    void Disparar()
    {
        if (bulletPrefab == null || firePoint == null)
        {
            Debug.LogWarning("Falta asignar");
            return;
        }

        GameObject burbuja = Instantiate(bulletPrefab, firePoint.position + firePoint.forward * 1.0f, firePoint.rotation);
        burbuja.transform.Rotate(90f, 0f, 0f); // la acuesta al instanciar
        Rigidbody rbBala = burbuja.GetComponent<Rigidbody>();

        if (rbBala != null)
        {
            rbBala.linearVelocity = firePoint.forward * bulletSpeed;
        }
    }
}
