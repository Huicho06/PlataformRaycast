using UnityEngine;
using System.Collections;

public class Controller2D : RaycastController
{
    // �ngulo m�ximo para las pendientes
    public float maxSlopeAngle = 80;
    // Informaci�n sobre las colisiones
    public CollisionInfo collisions;
    [HideInInspector]
    public Vector2 playerInput;

    public override void Start()
    {
        base.Start();
        collisions.faceDir = 1; // Inicializa la direcci�n del rostro

    }

    // M�todo para mover al jugador
    public void Move(Vector2 moveAmount, bool standingOnPlatform)
    {
        Move(moveAmount, Vector2.zero, standingOnPlatform);
    }

    public void Move(Vector2 moveAmount, Vector2 input, bool standingOnPlatform = false)
    {
        UpdateRaycastOrigins();// Actualiza los or�genes del raycast

        collisions.Reset();// Reinicia la informaci�n de colisiones
        collisions.moveAmountOld = moveAmount;
        playerInput = input; // Guarda la entrada del jugador
        // Si el movimiento es hacia abajo, desciende por la pendiente
        if (moveAmount.y < 0)
        {
            DescendSlope(ref moveAmount);
        }
        // Actualiza la direcci�n del rostro seg�n el movimiento en X
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
        // Si el jugador est� en una plataforma, actualiza el estado de colisi�n
        if (standingOnPlatform)
        {
            collisions.below = true;
        }
    }

    // Maneja las colisiones horizontales
    void HorizontalCollisions(ref Vector2 moveAmount)
    {
        float directionX = collisions.faceDir; // Direcci�n del movimiento en X
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

            Debug.DrawRay(rayOrigin, Vector2.right * directionX, Color.red); // Dibuja el rayo para depuraci�n

            if (hit) // Si hay colisi�n
            {
                if (hit.distance == 0)
                {
                    continue; // Si la colisi�n es instant�nea, contin�a
                }

                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up); // �ngulo de la pendiente

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

                    collisions.left = directionX == -1; // Colisi�n a la izquierda
                    collisions.right = directionX == 1; // Colisi�n a la derecha
                }
            }
        }
    }


    void VerticalCollisions(ref Vector2 moveAmount)
    {
        float directionY = Mathf.Sign(moveAmount.y); // Direcci�n del movimiento en Y
        float rayLength = Mathf.Abs(moveAmount.y) + skinWidth; // Longitud del raycast

        // Realiza raycasts para detectar colisiones verticales
        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + moveAmount.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

            Debug.DrawRay(rayOrigin, Vector2.up * directionY, Color.red); // Dibuja el rayo para depuraci�n

            if (hit)
            {
                // Maneja plataformas que se pueden atravesar
                if (hit.collider.tag == "Through")
                {
                    if (directionY == 1 || hit.distance == 0)
                    {
                        continue; // Si sube o est� en la parte superior, contin�a
                    }
                    if (collisions.fallingThroughPlatform)
                    {
                        continue; // Si ya est� cayendo a trav�s de la plataforma, contin�a
                    }
                    if (playerInput.y == -1)
                    {
                        collisions.fallingThroughPlatform = true; // Permite caer a trav�s
                        Invoke("ResetFallingThroughPlatform", .5f); // Resetea despu�s de medio segundo
                        continue;
                    }
                }

                moveAmount.y = (hit.distance - skinWidth) * directionY; // Ajusta el movimiento en Y
                rayLength = hit.distance; // Ajusta la longitud del raycast

                if (collisions.climbingSlope)
                {
                    moveAmount.x = moveAmount.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(moveAmount.x); // Ajusta el movimiento en X al escalar
                }

                collisions.below = directionY == -1; // Colisi�n por debajo
                collisions.above = directionY == 1; // Colisi�n por encima
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
                    collisions.slopeAngle = slopeAngle; // Actualiza el �ngulo de la pendiente
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
            collisions.below = true; // Colisi�n por debajo
            collisions.climbingSlope = true; // Indica que est� escalando
            collisions.slopeAngle = slopeAngle; // Guarda el �ngulo de la pendiente
            collisions.slopeNormal = slopeNormal; // Guarda la normal de la pendiente
        }
    }

    // Maneja el descenso por pendientes
    void DescendSlope(ref Vector2 moveAmount)
    {
        RaycastHit2D maxSlopeHitLeft = Physics2D.Raycast(raycastOrigins.bottomLeft, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, collisionMask);
        RaycastHit2D maxSlopeHitRight = Physics2D.Raycast(raycastOrigins.bottomRight, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, collisionMask);
        if (maxSlopeHitLeft ^ maxSlopeHitRight) // Si hay colisi�n en uno pero no en el otro
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

        // Si no hay colisi�n, ajusta el movimiento en Y
        if (!collisions.below)
        {
            collisions.descendingSlope = false; // No est� descendiendo por una pendiente
        }
    }

    // Maneja el descenso de la pendiente
    void HandleSlopeDescend(ref Vector2 moveAmount, RaycastHit2D hit)
    {
        float slopeAngle = Vector2.Angle(hit.normal, Vector2.up); // �ngulo de la pendiente
        if (slopeAngle <= maxSlopeAngle) // Si el �ngulo es menor o igual al �ngulo m�ximo
        {
            if (hit.distance < Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x)) // Si la distancia es menor que el valor calculado
            {
                float moveDistance = Mathf.Abs(moveAmount.x); // Distancia de movimiento
                float descendMoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance; // Ajusta el movimiento en Y
                moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x); // Ajusta el movimiento en X
                moveAmount.y -= descendMoveAmountY; // Aplica el descenso
                collisions.slopeAngle = slopeAngle; // Guarda el �ngulo de la pendiente
                collisions.below = true; // Colisi�n por debajo
                collisions.descendingSlope = true; // Indica que est� descendiendo
                collisions.slopeNormal = hit.normal; // Guarda la normal de la pendiente
            }
        }
    }

    void SlideDownMaxSlope(RaycastHit2D hit, ref Vector2 moveAmount)
    {
        // Verifica si hay un hit (colisi�n) v�lida.
        if (hit)
        {
            // Calcula el �ngulo de la pendiente usando la normal de la colisi�n.
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

            // Comprueba si el �ngulo de la pendiente es mayor que el �ngulo m�ximo permitido.
            if (slopeAngle > maxSlopeAngle)
            {
                // Ajusta el movimiento en el eje X para deslizarse por la pendiente.
                moveAmount.x = Mathf.Sign(hit.normal.x) * (Mathf.Abs(moveAmount.y) - hit.distance) / Mathf.Tan(slopeAngle * Mathf.Deg2Rad);

                // Actualiza el �ngulo de la pendiente en la informaci�n de colisi�n.
                collisions.slopeAngle = slopeAngle;

                // Indica que el jugador est� desliz�ndose por una pendiente m�xima.
                collisions.slidingDownMaxSlope = true;
                collisions.slopeNormal = hit.normal; // Almacena la normal de la pendiente.
            }
        }
    }

    void ResetFallingThroughPlatform()
    {
        // Reinicia el estado de ca�da a trav�s de plataformas, permitiendo al jugador volver a caer a trav�s de ellas.
        collisions.fallingThroughPlatform = false;
    }


    public struct CollisionInfo
    {
        // Indicadores de colisi�n en las direcciones vertical y horizontal.
        public bool above; // Si hay una colisi�n por encima del jugador.
        public bool below; // Si hay una colisi�n por debajo del jugador.
        public bool left;  // Si hay una colisi�n a la izquierda del jugador.
        public bool right; // Si hay una colisi�n a la derecha del jugador.

        // Indicadores para el manejo de pendientes.
        public bool climbingSlope; // Si el jugador est� escalando una pendiente.
        public bool descendingSlope; // Si el jugador est� descendiendo por una pendiente.
        public bool slidingDownMaxSlope; // Si el jugador se est� deslizando por una pendiente m�xima.

        // Informaci�n sobre la pendiente.
        public float slopeAngle; // �ngulo actual de la pendiente.
        public float slopeAngleOld; // �ngulo anterior de la pendiente.
        public Vector2 slopeNormal; // Normal de la pendiente actual.

        // Almacena el movimiento previo.
        public Vector2 moveAmountOld; // La cantidad de movimiento anterior antes de la colisi�n.

        // Direcci�n a la que el jugador est� mirando.
        public int faceDir; // Direcci�n en la que el jugador est� mirando (1 para la derecha, -1 para la izquierda).

        // Indica si el jugador est� cayendo a trav�s de plataformas.
        public bool fallingThroughPlatform; // Si el jugador est� en el proceso de caer a trav�s de una plataforma.

        // M�todo para reiniciar los valores de colisi�n.

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

            // Actualiza el �ngulo de la pendiente.
            slopeAngleOld = slopeAngle;
            slopeAngle = 0; // Reinicia el �ngulo de la pendiente actual.
        }
    }

}