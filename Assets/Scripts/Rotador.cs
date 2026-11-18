using UnityEngine;

public class Rotador : MonoBehaviour
{
    public float velocidadGiro = 60f;

    void Update()
    {
        transform.Rotate(0f, velocidadGiro * Time.deltaTime, 0f);
    }
}