using UnityEngine;
using System.Collections;

public class Controller2D : RaycastController
{
    // Ángulo máximo para las pendientes
    public float maxSlopeAngle = 80;
    // Información sobre las colisiones
    public CollisionInfo collisions;
    [HideInInspector]
    public Vector2 playerInput;

    public override void Start()
    {
        base.Start();
        collisions.faceDir = 1; // Inicializa la dirección del rostro

    }

    // Método para mover al jugador
    public void Move(Vector2 moveAmount, bool standingOnPlatform)
    {
        Move(moveAmount, Vector2.zero, standingOnPlatform);
    }

    public void Move(Vector2 moveAmount, Vector2 input, bool standingOnPlatform = false)
    {
        UpdateRaycastOrigins();// Actualiza los orígenes del raycast

        collisions.Reset();// Reinicia la información de colisiones
        collisions.moveAmountOld = moveAmount;
        playerInput = input; // Guarda la entrada del jugador
        // Si el movimiento es hacia abajo, desciende por la pendiente
        if (moveAmount.y < 0)
        {
            DescendSlope(ref moveAmount);
        }
        // Actualiza la dirección del rostro según el movimiento en X
        if (moveAmount.x != 0)
        {
            collisions.faceDir = (int)Mathf.Sign(moveAmount.x);
        }
        // Verifica colisiones horizontales
        HorizontalCollisions(ref moveAmount);
        // Verifica colisiones verticales
        if (moveAmount.y != 0)
        {
            VerticalCollisions(ref moveAmount);
        }

        transform.Translate(moveAmount);// Aplica el movimiento
        // Si el jugador está en una plataforma, actualiza el estado de colisión
        if (standingOnPlatform)
        {
            collisions.below = true;
        }
    }

    // Maneja las colisiones horizontales
    void HorizontalCollisions(ref Vector2 moveAmount)
    {
        float directionX = collisions.faceDir; // Dirección del movimiento en X
        float rayLength = Mathf.Abs(moveAmount.x) + skinWidth; // Longitud del raycast

        // Si el movimiento es menor que el ancho del skin, ajusta la longitud del raycast
        if (Mathf.Abs(moveAmount.x) < skinWidth)
        {
            rayLength = 2 * skinWidth;
        }

        // Realiza raycasts para detectar colisiones horizontales
        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.right * directionX, Color.red); // Dibuja el rayo para depuración

            if (hit) // Si hay colisión
            {
                if (hit.distance == 0)
                {
                    continue; // Si la colisión es instantánea, continúa
                }

                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up); // Ángulo de la pendiente

                // Maneja el escalamiento de pendientes
                if (i == 0 && slopeAngle <= maxSlopeAngle)
                {
                    if (collisions.descendingSlope)
                    {
                        collisions.descendingSlope = false;
                        moveAmount = collisions.moveAmountOld; // Reajusta el movimiento
                    }
                    float distanceToSlopeStart = 0;
                    if (slopeAngle != collisions.slopeAngleOld)
                    {
                        distanceToSlopeStart = hit.distance - skinWidth;
                        moveAmount.x -= distanceToSlopeStart * directionX; // Ajusta el movimiento
                    }
                    ClimbSlope(ref moveAmount, slopeAngle, hit.normal); // Escala la pendiente
                    moveAmount.x += distanceToSlopeStart * directionX; // Ajusta nuevamente
                }

                // Maneja colisiones normales
                if (!collisions.climbingSlope || slopeAngle > maxSlopeAngle)
                {
                    moveAmount.x = (hit.distance - skinWidth) * directionX; // Ajusta el movimiento en X
                    rayLength = hit.distance; // Ajusta la longitud del raycast

                    if (collisions.climbingSlope)
                    {
                        moveAmount.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x); // Ajusta el movimiento en Y
                    }

                    collisions.left = directionX == -1; // Colisión a la izquierda
                    collisions.right = directionX == 1; // Colisión a la derecha
                }
            }
        }
    }


    void VerticalCollisions(ref Vector2 moveAmount)
    {
        float directionY = Mathf.Sign(moveAmount.y); // Dirección del movimiento en Y
        float rayLength = Mathf.Abs(moveAmount.y) + skinWidth; // Longitud del raycast

        // Realiza raycasts para detectar colisiones verticales
        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + moveAmount.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.up * directionY, Color.red); // Dibuja el rayo para depuración

            if (hit)
            {
                // Maneja plataformas que se pueden atravesar
                if (hit.collider.tag == "Through")
                {
                    if (directionY == 1 || hit.distance == 0)
                    {
                        continue; // Si sube o está en la parte superior, continúa
                    }
                    if (collisions.fallingThroughPlatform)
                    {
                        continue; // Si ya está cayendo a través de la plataforma, continúa
                    }
                    if (playerInput.y == -1)
                    {
                        collisions.fallingThroughPlatform = true; // Permite caer a través
                        Invoke("ResetFallingThroughPlatform", .5f); // Resetea después de medio segundo
                        continue;
                    }
                }

                moveAmount.y = (hit.distance - skinWidth) * directionY; // Ajusta el movimiento en Y
                rayLength = hit.distance; // Ajusta la longitud del raycast

                if (collisions.climbingSlope)
                {
                    moveAmount.x = moveAmount.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(moveAmount.x); // Ajusta el movimiento en X al escalar
                }

                collisions.below = directionY == -1; // Colisión por debajo
                collisions.above = directionY == 1; // Colisión por encima
            }
        }

        // Maneja el escalamiento de pendientes
        if (collisions.climbingSlope)
        {
            float directionX = Mathf.Sign(moveAmount.x);
            rayLength = Mathf.Abs(moveAmount.x) + skinWidth;
            Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * moveAmount.y;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != collisions.slopeAngle)
                {
                    moveAmount.x = (hit.distance - skinWidth) * directionX; // Ajusta el movimiento en X
                    collisions.slopeAngle = slopeAngle; // Actualiza el ángulo de la pendiente
                    collisions.slopeNormal = hit.normal; // Guarda la normal de la pendiente
                }
            }
        }
    }

    // Maneja el escalamiento de pendientes
    void ClimbSlope(ref Vector2 moveAmount, float slopeAngle, Vector2 slopeNormal)
    {
        float moveDistance = Mathf.Abs(moveAmount.x);
        float climbmoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

        if (moveAmount.y <= climbmoveAmountY) // Si el movimiento en Y es menor o igual al movimiento en pendiente
        {
            moveAmount.y = climbmoveAmountY; // Ajusta el movimiento en Y
            moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x); // Ajusta el movimiento en X
            collisions.below = true; // Colisión por debajo
            collisions.climbingSlope = true; // Indica que está escalando
            collisions.slopeAngle = slopeAngle; // Guarda el ángulo de la pendiente
            collisions.slopeNormal = slopeNormal; // Guarda la normal de la pendiente
        }
    }

    // Maneja el descenso por pendientes
    void DescendSlope(ref Vector2 moveAmount)
    {
        RaycastHit2D maxSlopeHitLeft = Physics2D.Raycast(raycastOrigins.bottomLeft, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, collisionMask);
        RaycastHit2D maxSlopeHitRight = Physics2D.Raycast(raycastOrigins.bottomRight, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, collisionMask);
        if (maxSlopeHitLeft ^ maxSlopeHitRight) // Si hay colisión en uno pero no en el otro
        {
            if (maxSlopeHitLeft)
            {
                HandleSlopeDescend(ref moveAmount, maxSlopeHitLeft); // Maneja el descenso de la pendiente
            }
            else
            {
                HandleSlopeDescend(ref moveAmount, maxSlopeHitRight); // Maneja el descenso de la pendiente
            }
        }

        // Si no hay colisión, ajusta el movimiento en Y
        if (!collisions.below)
        {
            collisions.descendingSlope = false; // No está descendiendo por una pendiente
        }
    }

    // Maneja el descenso de la pendiente
    void HandleSlopeDescend(ref Vector2 moveAmount, RaycastHit2D hit)
    {
        float slopeAngle = Vector2.Angle(hit.normal, Vector2.up); // Ángulo de la pendiente
        if (slopeAngle <= maxSlopeAngle) // Si el ángulo es menor o igual al ángulo máximo
        {
            if (hit.distance < Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x)) // Si la distancia es menor que el valor calculado
            {
                float moveDistance = Mathf.Abs(moveAmount.x); // Distancia de movimiento
                float descendMoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance; // Ajusta el movimiento en Y
                moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x); // Ajusta el movimiento en X
                moveAmount.y -= descendMoveAmountY; // Aplica el descenso
                collisions.slopeAngle = slopeAngle; // Guarda el ángulo de la pendiente
                collisions.below = true; // Colisión por debajo
                collisions.descendingSlope = true; // Indica que está descendiendo
                collisions.slopeNormal = hit.normal; // Guarda la normal de la pendiente
            }
        }
    }

    void SlideDownMaxSlope(RaycastHit2D hit, ref Vector2 moveAmount)
    {
        // Verifica si hay un hit (colisión) válida.
        if (hit)
        {
            // Calcula el ángulo de la pendiente usando la normal de la colisión.
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

            // Comprueba si el ángulo de la pendiente es mayor que el ángulo máximo permitido.
            if (slopeAngle > maxSlopeAngle)
            {
                // Ajusta el movimiento en el eje X para deslizarse por la pendiente.
                moveAmount.x = Mathf.Sign(hit.normal.x) * (Mathf.Abs(moveAmount.y) - hit.distance) / Mathf.Tan(slopeAngle * Mathf.Deg2Rad);

                // Actualiza el ángulo de la pendiente en la información de colisión.
                collisions.slopeAngle = slopeAngle;

                // Indica que el jugador está deslizándose por una pendiente máxima.
                collisions.slidingDownMaxSlope = true;
                collisions.slopeNormal = hit.normal; // Almacena la normal de la pendiente.
            }
        }
    }

    void ResetFallingThroughPlatform()
    {
        // Reinicia el estado de caída a través de plataformas, permitiendo al jugador volver a caer a través de ellas.
        collisions.fallingThroughPlatform = false;
    }


    public struct CollisionInfo
    {
        // Indicadores de colisión en las direcciones vertical y horizontal.
        public bool above; // Si hay una colisión por encima del jugador.
        public bool below; // Si hay una colisión por debajo del jugador.
        public bool left;  // Si hay una colisión a la izquierda del jugador.
        public bool right; // Si hay una colisión a la derecha del jugador.

        // Indicadores para el manejo de pendientes.
        public bool climbingSlope; // Si el jugador está escalando una pendiente.
        public bool descendingSlope; // Si el jugador está descendiendo por una pendiente.
        public bool slidingDownMaxSlope; // Si el jugador se está deslizando por una pendiente máxima.

        // Información sobre la pendiente.
        public float slopeAngle; // Ángulo actual de la pendiente.
        public float slopeAngleOld; // Ángulo anterior de la pendiente.
        public Vector2 slopeNormal; // Normal de la pendiente actual.

        // Almacena el movimiento previo.
        public Vector2 moveAmountOld; // La cantidad de movimiento anterior antes de la colisión.

        // Dirección a la que el jugador está mirando.
        public int faceDir; // Dirección en la que el jugador está mirando (1 para la derecha, -1 para la izquierda).

        // Indica si el jugador está cayendo a través de plataformas.
        public bool fallingThroughPlatform; // Si el jugador está en el proceso de caer a través de una plataforma.

        // Método para reiniciar los valores de colisión.

        public void Reset()
        {
            // Reinicia las colisiones.
            above = below = false;
            left = right = false;

            // Reinicia los indicadores de pendiente.
            climbingSlope = false;
            descendingSlope = false;
            slidingDownMaxSlope = false;
            slopeNormal = Vector2.zero; // Reinicia la normal de la pendiente.

            // Actualiza el ángulo de la pendiente.
            slopeAngleOld = slopeAngle;
            slopeAngle = 0; // Reinicia el ángulo de la pendiente actual.
        }
    }

}