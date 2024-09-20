using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider2D))]
public class RaycastController : MonoBehaviour
{
    // Máscara de capas para determinar qué capas se detectan con raycasts.
    public LayerMask collisionMask;

    // Ancho de la piel que se usa para expandir los límites del collider.
    public const float skinWidth = .015f;
    const float dstBetweenRays = .25f; // Distancia entre rayos de colisión.

    // Contadores para la cantidad de rayos horizontales y verticales.
    [HideInInspector]
    public int horizontalRayCount;
    [HideInInspector]
    public int verticalRayCount;

    // Espaciado entre los rayos horizontales y verticales.
    [HideInInspector]
    public float horizontalRaySpacing;
    [HideInInspector]
    public float verticalRaySpacing;

    // Referencia al BoxCollider2D del objeto.
    [HideInInspector]
    public BoxCollider2D collider;

    // Estructura para almacenar los orígenes de los raycasts.
    public RaycastOrigins raycastOrigins;

    public virtual void Awake()
    {
        // Obtiene el BoxCollider2D al iniciar el script.
        collider = GetComponent<BoxCollider2D>();
    }

    public virtual void Start()
    {
        // Calcula el espaciado de los rayos al inicio del juego.
        CalculateRaySpacing();
    }

    public void UpdateRaycastOrigins()
    {
        // Obtiene los límites del collider y los expande según el skinWidth.
        Bounds bounds = collider.bounds;
        bounds.Expand(skinWidth * -2);

        // Establece las posiciones de los cuatro vértices del collider.
        raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
        raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }

    public void CalculateRaySpacing()
    {
        // Obtiene los límites del collider y los expande según el skinWidth.
        Bounds bounds = collider.bounds;
        bounds.Expand(skinWidth * -2);

        // Calcula el ancho y la altura del collider.
        float boundsWidth = bounds.size.x;
        float boundsHeight = bounds.size.y;

        // Calcula la cantidad de rayos horizontales y verticales en función del tamaño del collider.
        horizontalRayCount = Mathf.RoundToInt(boundsHeight / dstBetweenRays);
        verticalRayCount = Mathf.RoundToInt(boundsWidth / dstBetweenRays);

        // Calcula el espaciado entre los rayos.
        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }

    // Estructura para almacenar las posiciones de los orígenes de los raycasts.
    public struct RaycastOrigins
    {
        public Vector2 topLeft, topRight; // Vértices superiores.
        public Vector2 bottomLeft, bottomRight; // Vértices inferiores.
    }
}
