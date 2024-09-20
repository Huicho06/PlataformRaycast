using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Clase que controla el movimiento de plataformas, hereda de RaycastController
public class PlatformController : RaycastController
{
    public LayerMask passengerMask; // Máscara de capa para los pasajeros

    public Vector3[] localWaypoints; // Puntos de referencia locales de la plataforma
    Vector3[] globalWaypoints; // Puntos de referencia globales de la plataforma

    public float speed; // Velocidad de movimiento de la plataforma
    public bool cyclic; // Indica si la plataforma debe moverse en un ciclo
    public float waitTime; // Tiempo de espera al alcanzar un punto
    [Range(0, 2)]
    public float easeAmount; // Cantidad de suavizado al mover la plataforma

    int fromWaypointIndex; // Índice del punto de partida
    float percentBetweenWaypoints; // Porcentaje entre puntos de referencia
    float nextMoveTime; // Tiempo hasta el próximo movimiento

    List<PassengerMovement> passengerMovement; // Lista de movimientos de los pasajeros
    Dictionary<Transform, Controller2D> passengerDictionary = new Dictionary<Transform, Controller2D>(); // Diccionario para los pasajeros

    // Método de inicio, inicializa los puntos de referencia globales
    public override void Start()
    {
        base.Start();
        globalWaypoints = new Vector3[localWaypoints.Length];
        for (int i = 0; i < globalWaypoints.Length; i++)
        {
            globalWaypoints[i] = localWaypoints[i] + transform.position; // Convierte los puntos locales a globales
        }
    }

    // Método de actualización, se llama cada cuadro
    void Update()
    {
        UpdateRaycastOrigins(); // Actualiza los orígenes de raycast

        Vector3 velocity = CalculatePlatformMovement(); // Calcula el movimiento de la plataforma

        CalculatePassengerMovement(velocity); // Calcula el movimiento de los pasajeros

        MovePassengers(true); // Mueve a los pasajeros antes de mover la plataforma
        transform.Translate(velocity); // Mueve la plataforma
        MovePassengers(false); // Mueve a los pasajeros después de mover la plataforma
    }

    // Método para aplicar una función de suavizado
    float Ease(float x)
    {
        float a = easeAmount + 1;
        return Mathf.Pow(x, a) / (Mathf.Pow(x, a) + Mathf.Pow(1 - x, a)); // Función de suavizado
    }

    // Método que calcula el movimiento de la plataforma
    Vector3 CalculatePlatformMovement()
    {
        // Si no es el momento de mover, regresa cero
        if (Time.time < nextMoveTime)
        {
            return Vector3.zero;
        }

        // Calcula el índice del siguiente punto
        fromWaypointIndex %= globalWaypoints.Length; // Asegura que el índice no exceda el número de puntos
        int toWaypointIndex = (fromWaypointIndex + 1) % globalWaypoints.Length; // Índice del próximo punto
        float distanceBetweenWaypoints = Vector3.Distance(globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointIndex]); // Distancia entre puntos
        percentBetweenWaypoints += Time.deltaTime * speed / distanceBetweenWaypoints; // Incrementa el porcentaje de movimiento
        percentBetweenWaypoints = Mathf.Clamp01(percentBetweenWaypoints); // Limita el valor entre 0 y 1
        float easedPercentBetweenWaypoints = Ease(percentBetweenWaypoints); // Aplica suavizado

        // Calcula la nueva posición
        Vector3 newPos = Vector3.Lerp(globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointIndex], percentBetweenWaypoints);

        // Comprueba si ha llegado al siguiente punto
        if (percentBetweenWaypoints >= 1)
        {
            percentBetweenWaypoints = 0; // Reinicia el porcentaje
            fromWaypointIndex++; // Avanza al siguiente índice

            // Si no es cíclico, revierte la dirección al alcanzar el final
            if (!cyclic)
            {
                if (fromWaypointIndex >= globalWaypoints.Length - 1)
                {
                    fromWaypointIndex = 0;
                    System.Array.Reverse(globalWaypoints); // Invierte la dirección
                }
            }
            nextMoveTime = Time.time + waitTime; // Establece el tiempo de espera
        }

        return newPos - transform.position; // Retorna el cambio de posición
    }

    // Método para mover a los pasajeros
    void MovePassengers(bool beforeMovePlatform)
    {
        foreach (PassengerMovement passenger in passengerMovement)
        {
            // Añade el pasajero al diccionario si no está ya
            if (!passengerDictionary.ContainsKey(passenger.transform))
            {
                passengerDictionary.Add(passenger.transform, passenger.transform.GetComponent<Controller2D>());
            }

            // Mueve al pasajero si es el momento adecuado
            if (passenger.moveBeforePlatform == beforeMovePlatform)
            {
                passengerDictionary[passenger.transform].Move(passenger.velocity, passenger.standingOnPlatform);
            }
        }
    }

    // Método para calcular el movimiento de los pasajeros en la plataforma
    void CalculatePassengerMovement(Vector3 velocity)
    {
        HashSet<Transform> movedPassengers = new HashSet<Transform>(); // Conjunto para evitar mover a los mismos pasajeros varias veces
        passengerMovement = new List<PassengerMovement>(); // Inicializa la lista de movimientos de pasajeros

        float directionX = Mathf.Sign(velocity.x); // Determina la dirección horizontal
        float directionY = Mathf.Sign(velocity.y); // Determina la dirección vertical

        // Plataforma en movimiento vertical
        if (velocity.y != 0)
        {
            float rayLength = Mathf.Abs(velocity.y) + skinWidth; // Longitud del rayo

            for (int i = 0; i < verticalRayCount; i++)
            {
                Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft; // Origen del rayo
                rayOrigin += Vector2.right * (verticalRaySpacing * i); // Ajusta la posición del rayo
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, passengerMask); // Lanza el rayo

                // Si el rayo impacta
                if (hit && hit.distance != 0)
                {
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform); // Añade el pasajero al conjunto
                        float pushX = (directionY == 1) ? velocity.x : 0; // Calcula el desplazamiento en X
                        float pushY = velocity.y - (hit.distance - skinWidth) * directionY; // Calcula el desplazamiento en Y

                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), directionY == 1, true)); // Añade el movimiento del pasajero
                    }
                }
            }
        }

        // Plataforma en movimiento horizontal
        if (velocity.x != 0)
        {
            float rayLength = Mathf.Abs(velocity.x) + skinWidth; // Longitud del rayo

            for (int i = 0; i < horizontalRayCount; i++)
            {
                Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight; // Origen del rayo
                rayOrigin += Vector2.up * (horizontalRaySpacing * i); // Ajusta la posición del rayo
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, passengerMask); // Lanza el rayo

                // Si el rayo impacta
                if (hit && hit.distance != 0)
                {
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform); // Añade el pasajero al conjunto
                        float pushX = velocity.x - (hit.distance - skinWidth) * directionX; // Calcula el desplazamiento en X
                        float pushY = -skinWidth; // Desplazamiento vertical fijo

                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), false, true)); // Añade el movimiento del pasajero
                    }
                }
            }
        }

        // Pasajero en la parte superior de una plataforma que se mueve horizontalmente o hacia abajo
        if (directionY == -1 || velocity.y == 0 && velocity.x != 0)
        {
            float rayLength = skinWidth * 2; // Longitud del rayo

            for (int i = 0; i < verticalRayCount; i++)
            {
                Vector2 rayOrigin = raycastOrigins.topLeft + Vector2.right * (verticalRaySpacing * i); // Origen del rayo
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, rayLength, passengerMask); // Lanza el rayo

                // Si el rayo impacta
                if (hit && hit.distance != 0)
                {
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform); // Añade el pasajero al conjunto
                        float pushX = velocity.x; // Desplazamiento en X
                        float pushY = velocity.y; // Desplazamiento en Y

                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), true, false)); // Añade el movimiento del pasajero
                    }
                }
            }
        }
    }

    // Estructura que representa el movimiento de un pasajero
    struct PassengerMovement
    {
        public Transform transform; // Transform del pasajero
        public Vector3 velocity; // Velocidad del pasajero
        public bool standingOnPlatform; // Indica si el pasajero está sobre la plataforma
        public bool moveBeforePlatform; // Indica si se mueve antes de la plataforma

        // Constructor de la estructura
        public PassengerMovement(Transform _transform, Vector3 _velocity, bool _standingOnPlatform, bool _moveBeforePlatform)
        {
            transform = _transform;
            velocity = _velocity;
            standingOnPlatform = _standingOnPlatform;
            moveBeforePlatform = _moveBeforePlatform;
        }
    }

    // Método para dibujar gizmos en el editor
    private void OnDrawGizmos()
    {
        if (localWaypoints != null)
        {
            Gizmos.color = Color.red; // Color de los gizmos
            float size = .3f;

            // Dibuja líneas entre los puntos de referencia
            for (int i = 0; i < localWaypoints.Length; i++)
            {
                Vector3 globalWaypoinPos = (Application.isPlaying) ? globalWaypoints[i] : localWaypoints[i] + transform.position;
                Gizmos.DrawLine(globalWaypoinPos - Vector3.up * size, globalWaypoinPos + Vector3.up * size);
                Gizmos.DrawLine(globalWaypoinPos - Vector3.left * size, globalWaypoinPos + Vector3.left * size);
            }
        }
    }
}
