using UnityEngine;
using UnityEngine.UI;

public class movimiento : MonoBehaviour
{
    private Rigidbody rb;

    [Header("Movimiento")]
    public float velocidad = 4f;
    public float velocidadSprint = 7f;
    public float aceleracion = 12f;

    [Header("Modelo del osito")]
    public Transform modeloOsito;
    public Transform brazoIzquierdo;
    public Transform brazoDerecho;
    public Transform pataIzquierda;
    public Transform pataDerecha;

    [Header("Animación")]
    public float velocidadGiro = 540f;
    public float anguloBrazos = 25f;
    public float anguloPatas = 20f;
    public float frecuenciaPasos = 2f;

    private Transform[] extremidades;
    private Quaternion[] rotacionesOriginales;
    private Quaternion rotacionInicialModelo;
    private float fasePaso;
    private float intensidadAnimacion;

    [Header("Caída al vacío")]
    public float limiteCaida = -5f;
    private Vector3 posicionInicial;
    private Quaternion rotacionInicial;

    [Header("Puntaje")]
    public Text puntaje;
    [Min(12)] public int metaPuntos = 15;

    private int contador_coleccionables = 0;
    private bool reinicioPendiente;

    private GameObject[] coleccionables;
    private Vector3[] posicionesColeccionables;
    private Quaternion[] rotacionesColeccionables;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        posicionInicial = rb.position;
        rotacionInicial = rb.rotation;

        if (modeloOsito != null)
        {
            rotacionInicialModelo = modeloOsito.localRotation;
        }

        extremidades = new Transform[]
        {
            brazoIzquierdo,
            brazoDerecho,
            pataIzquierda,
            pataDerecha
        };

        rotacionesOriginales = new Quaternion[extremidades.Length];

        for (int i = 0; i < extremidades.Length; i++)
        {
            if (extremidades[i] != null)
            {
                rotacionesOriginales[i] =
                    extremidades[i].localRotation;
            }
        }

        GuardarColeccionables();
        ActualizarPuntaje();
    }

    private void GuardarColeccionables()
    {
        // Los dulces deben estar activos al iniciar la partida.
        GameObject[] caramelos =
            GameObject.FindGameObjectsWithTag("Coleccionable");

        GameObject[] paletas =
            GameObject.FindGameObjectsWithTag("Especial");

        coleccionables = new GameObject[
            caramelos.Length + paletas.Length
        ];

        caramelos.CopyTo(coleccionables, 0);
        paletas.CopyTo(coleccionables, caramelos.Length);

        posicionesColeccionables =
            new Vector3[coleccionables.Length];

        rotacionesColeccionables =
            new Quaternion[coleccionables.Length];

        for (int i = 0; i < coleccionables.Length; i++)
        {
            posicionesColeccionables[i] =
                coleccionables[i].transform.position;

            rotacionesColeccionables[i] =
                coleccionables[i].transform.rotation;
        }
    }

    private void FixedUpdate()
    {
        // Reinicia fuera del evento de contacto con el último dulce.
        if (reinicioPendiente)
        {
            ReiniciarPartida();
            return;
        }

        if (rb.position.y < limiteCaida)
        {
            VolverAlInicio();
            Debug.Log("El jugador cayó y volvió al inicio.");
            return;
        }

        float movimientoH = Input.GetAxis("Horizontal");
        float movimientoV = Input.GetAxis("Vertical");

        Vector3 direccion = new Vector3(
            movimientoH, 0f, movimientoV
        );

        direccion = Vector3.ClampMagnitude(direccion, 1f);

        bool sprint = Input.GetKey(KeyCode.LeftShift);
        float rapidez = sprint ? velocidadSprint : velocidad;

        Vector3 velocidadObjetivo = direccion * rapidez;

        Vector3 velocidadHorizontal = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z
        );

        Vector3 cambio = velocidadObjetivo - velocidadHorizontal;

        cambio = Vector3.ClampMagnitude(
            cambio,
            aceleracion * Time.fixedDeltaTime
        );

        rb.AddForce(cambio, ForceMode.VelocityChange);
    }

    private void LateUpdate()
    {
        AnimarOsito();
    }

    private void AnimarOsito()
    {
        Vector3 movimientoHorizontal = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z
        );

        float rapidezActual = movimientoHorizontal.magnitude;
        bool seMueve = rapidezActual > 0.15f;

        // Gira únicamente el modelo visual.
        if (modeloOsito != null && seMueve)
        {
            Quaternion rotacionObjetivo = Quaternion.LookRotation(
                movimientoHorizontal.normalized,
                Vector3.up
            );

            modeloOsito.rotation = Quaternion.RotateTowards(
                modeloOsito.rotation,
                rotacionObjetivo,
                velocidadGiro * Time.deltaTime
            );
        }

        float intensidadObjetivo = seMueve
            ? Mathf.Clamp01(
                rapidezActual / Mathf.Max(velocidad, 0.1f)
            )
            : 0f;

        intensidadAnimacion = Mathf.MoveTowards(
            intensidadAnimacion,
            intensidadObjetivo,
            5f * Time.deltaTime
        );

        if (seMueve)
        {
            float factorRapidez = Mathf.Clamp(
                rapidezActual / Mathf.Max(velocidad, 0.1f),
                0.4f,
                2f
            );

            fasePaso += Time.deltaTime
                * frecuenciaPasos
                * factorRapidez
                * Mathf.PI * 2f;
        }

        float balanceo =
            Mathf.Sin(fasePaso) * intensidadAnimacion;

        AplicarBalanceo(0, balanceo * anguloBrazos);
        AplicarBalanceo(1, -balanceo * anguloBrazos);
        AplicarBalanceo(2, -balanceo * anguloPatas);
        AplicarBalanceo(3, balanceo * anguloPatas);

        if (!seMueve && intensidadAnimacion == 0f)
        {
            fasePaso = 0f;
        }
    }

    private void AplicarBalanceo(int indice, float angulo)
    {
        if (extremidades[indice] == null)
        {
            return;
        }

        extremidades[indice].localRotation =
            rotacionesOriginales[indice]
            * Quaternion.Euler(angulo, 0f, 0f);
    }

    private void VolverAlInicio()
    {
        // Elimina el impulso y el giro.
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.position = posicionInicial;
        rb.rotation = rotacionInicial;

        fasePaso = 0f;
        intensidadAnimacion = 0f;

        if (modeloOsito != null)
        {
            modeloOsito.localRotation = rotacionInicialModelo;
        }

        for (int i = 0; i < extremidades.Length; i++)
        {
            if (extremidades[i] != null)
            {
                extremidades[i].localRotation =
                    rotacionesOriginales[i];
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (reinicioPendiente ||
            !other.gameObject.activeInHierarchy)
        {
            return;
        }

        int puntosGanados;

        if (other.CompareTag("Coleccionable"))
        {
            puntosGanados = 1;
        }
        else if (other.CompareTag("Especial"))
        {
            puntosGanados = 3;
        }
        else
        {
            return;
        }

        contador_coleccionables += puntosGanados;
        other.gameObject.SetActive(false);

        Debug.Log(
            "Recogido: " + other.gameObject.name +
            " | +" + puntosGanados +
            " puntos | Total: " + contador_coleccionables
        );

        ActualizarPuntaje();

        if (contador_coleccionables >= metaPuntos)
        {
            reinicioPendiente = true;
            Debug.Log("¡Meta alcanzada! Reiniciando la partida.");
        }
    }

    private void ReiniciarPartida()
    {
        VolverAlInicio();

        // Recupera todos los dulces y sus posiciones originales.
        for (int i = 0; i < coleccionables.Length; i++)
        {
            if (coleccionables[i] != null)
            {
                coleccionables[i].transform.SetPositionAndRotation(
                    posicionesColeccionables[i],
                    rotacionesColeccionables[i]
                );

                coleccionables[i].SetActive(true);
            }
        }

        contador_coleccionables = 0;
        reinicioPendiente = false;

        ActualizarPuntaje();

        Debug.Log(
            "Nueva partida: dulces restaurados y puntaje en cero."
        );
    }

    private void ActualizarPuntaje()
    {
        if (puntaje != null)
        {
            puntaje.text = "Dulces: " +
                contador_coleccionables + " / " + metaPuntos;
        }
    }
}