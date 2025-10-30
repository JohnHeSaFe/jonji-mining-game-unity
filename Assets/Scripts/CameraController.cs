using UnityEngine;

public class ConfiguracionCamara : MonoBehaviour
{
    public Vector3 posicionInicial = new Vector3(10.4f, -2.45f, -17.4f);
    public float sizeInicial = 9.09f;

    private Camera cam;
    private Transform camTransform;

    void Awake() 
    {
        cam = GetComponent<Camera>();
        camTransform = GetComponent<Transform>();

        camTransform.position = posicionInicial;

        cam.orthographic = true; 
        cam.orthographicSize = sizeInicial;
    }
}