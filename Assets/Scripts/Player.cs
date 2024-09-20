using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Controller2D))]
public class Player : MonoBehaviour
{
    public float maxJumpHeight = 4; // Altura máxima del salto
    public float minJumpHeight = 1; // Altura mínima del salto
    public float timeToJumpApex = .4f; // Tiempo para alcanzar el punto más alto del salto
    float accelerationTimeAirborne = .2f; // Tiempo de aceleración en el aire
    float accelerationTimeGrounded = .1f; // Tiempo de aceleración en el suelo
    float moveSpeed = 6; // Velocidad de movimiento

    public Vector2 wallJumpClimb; // Velocidad de salto al escalar una pared
    public Vector2 wallJumpOff; // Velocidad de salto al despegar de una pared
    public Vector2 wallLeap; // Velocidad de salto al saltar desde una pared

    public float wallSlideSpeedMax = 3; // Velocidad máxima al deslizarse por una pared
    public float wallStickTime = .25f; // Tiempo en que el jugador se queda pegado a la pared
    float timeToWallUnstick; // Temporizador para dejar de estar pegado a la pared

    float gravity; // Gravedad
    float maxJumpVelocity; // Velocidad máxima de salto
    float minJumpVelocity; // Velocidad mínima de salto
    Vector3 velocity; // Velocidad del jugador
    float velocityXSmoothing; // Suavizado de la velocidad en X

    Controller2D controller; // Controlador del personaje

    Vector2 directionalInput; // Entrada direccional del jugador
    bool wallSliding; // Indica si el jugador se está deslizando por una pared
    int wallDirX; // Dirección de la pared en la que está el jugador

    void Start()
    {
        controller = GetComponent<Controller2D>();

        // Calcula la gravedad y las velocidades de salto
        gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
    }

    void Update()
    {
        CalculateVelocity(); // Calcula la velocidad del jugador
        HandleWallSliding(); // Maneja el deslizamiento por la pared

        // Mueve el jugador utilizando el controlador
        controller.Move(velocity * Time.deltaTime, directionalInput);

        // Si el jugador colisiona con el suelo o el techo
        if (controller.collisions.above || controller.collisions.below)
        {
            // Si está deslizándose por una pendiente
            if (controller.collisions.slidingDownMaxSlope)
            {
                velocity.y += controller.collisions.slopeNormal.y * -gravity * Time.deltaTime; // Ajusta la velocidad vertical
            }
            else
            {
                velocity.y = 0; // Resetea la velocidad vertical
            }
        }
    }

    public void SetDirectionalInput(Vector2 input)
    {
        directionalInput = input; // Establece la entrada direccional
    }

    public void OnJumpInputDown()
    {
        // Maneja la entrada de salto
        if (wallSliding)
        {
            if (wallDirX == directionalInput.x) // Si el jugador salta en la dirección de la pared
            {
                velocity.x = -wallDirX * wallJumpClimb.x; // Ajusta la velocidad en X
                velocity.y = wallJumpClimb.y; // Ajusta la velocidad de salto
            }
            else if (directionalInput.x == 0) // Si no hay entrada horizontal
            {
                velocity.x = -wallDirX * wallJumpOff.x; // Ajusta la velocidad de salto al despegar
                velocity.y = wallJumpOff.y; // Ajusta la velocidad de salto
            }
            else // Si salta en dirección opuesta a la pared
            {
                velocity.x = -wallDirX * wallLeap.x; // Ajusta la velocidad de salto
                velocity.y = wallLeap.y; // Ajusta la velocidad de salto
            }
        }

        // Si el jugador está en el suelo
        if (controller.collisions.below)
        {
            if (controller.collisions.slidingDownMaxSlope) // Si está deslizándose por una pendiente
            {
                if (directionalInput.x != -Mathf.Sign(controller.collisions.slopeNormal.x))
                { // Si no salta contra la pendiente
                    velocity.y = maxJumpVelocity * controller.collisions.slopeNormal.y; // Ajusta la velocidad vertical
                    velocity.x = maxJumpVelocity * controller.collisions.slopeNormal.x; // Ajusta la velocidad horizontal
                }
            }
            else
            {
                velocity.y = maxJumpVelocity; // Establece la velocidad de salto
            }
        }
    }

    public void OnJumpInputUp()
    {
        // Maneja la liberación de la entrada de salto
        if (velocity.y > minJumpVelocity)
        {
            velocity.y = minJumpVelocity; // Limita la velocidad de salto
        }
    }

    void HandleWallSliding()
    {
        wallDirX = (controller.collisions.left) ? -1 : 1; // Determina la dirección de la pared
        wallSliding = false; // Reinicia el estado de deslizamiento

        // Si el jugador está contra una pared y no está en el suelo
        if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0)
        {
            wallSliding = true; // Activa el deslizamiento

            // Limita la velocidad de deslizamiento
            if (velocity.y < -wallSlideSpeedMax)
            {
                velocity.y = -wallSlideSpeedMax;
            }

            // Maneja el tiempo de pegado a la pared
            if (timeToWallUnstick > 0)
            {
                velocityXSmoothing = 0; // Reinicia el suavizado de velocidad
                velocity.x = 0; // Reinicia la velocidad en X

                // Verifica la entrada direccional
                if (directionalInput.x != wallDirX && directionalInput.x != 0)
                {
                    timeToWallUnstick -= Time.deltaTime; // Reduce el tiempo
                }
                else
                {
                    timeToWallUnstick = wallStickTime; // Resetea el temporizador
                }
            }
            else
            {
                timeToWallUnstick = wallStickTime; // Resetea el temporizador
            }
        }
    }

    void CalculateVelocity()
    {
        float targetVelocityX = directionalInput.x * moveSpeed; // Calcula la velocidad objetivo en X
        // Suaviza la velocidad en X
        velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, (controller.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirborne);
        velocity.y += gravity * Time.deltaTime; // Aplica gravedad
    }
}
