using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class CameraFollow : MonoBehaviour
{
    // Referencia al controlador del personaje objetivo.
    public Controller2D target;

    // Desplazamiento vertical de la cámara respecto al personaje.
    public float verticalOffset;

    // Distancia de anticipación en el eje X.
    public float lookAheadDstX;

    // Tiempo para suavizar el movimiento anticipado en el eje X.
    public float lookSmoothTimeX;

    // Tiempo para suavizar el movimiento vertical.
    public float verticalSmoothTime;

    // Tamaño del área de enfoque.
    public Vector2 focusAreaSize;

    // Objeto que gestiona el área de enfoque.
    FocusArea focusArea;

    // Variables para la anticipación del movimiento.
    float currentLookAheadX;
    float targetLookAheadX;
    float lookAheadDirX;
    float smoothLookVelocityX;
    float smoothVelocityY;

    // Bandera para saber si la anticipación se ha detenido.
    bool lookAheadStopped;

    void Start()
    {
        // Inicializa el área de enfoque con los límites del collider del objetivo y el tamaño definido.
        focusArea = new FocusArea(target.collider.bounds, focusAreaSize);
    }

    void LateUpdate()
    {
        // Actualiza el área de enfoque con los límites actuales del objetivo.
        focusArea.Update(target.collider.bounds);

        // Calcula la posición focal de la cámara.
        Vector2 focusPosition = focusArea.centre + Vector2.up * verticalOffset;

        // Maneja la anticipación del movimiento en el eje X.
        if (focusArea.velocity.x != 0)
        {
            lookAheadDirX = Mathf.Sign(focusArea.velocity.x);
            if (Mathf.Sign(target.playerInput.x) == Mathf.Sign(focusArea.velocity.x) && target.playerInput.x != 0)
            {
                lookAheadStopped = false;
                targetLookAheadX = lookAheadDirX * lookAheadDstX; // Establece la anticipación.
            }
            else
            {
                // Si se detiene la anticipación, suaviza el movimiento.
                if (!lookAheadStopped)
                {
                    lookAheadStopped = true;
                    targetLookAheadX = currentLookAheadX + (lookAheadDirX * lookAheadDstX - currentLookAheadX) / 4f;
                }
            }
        }

        // Suaviza la anticipación del movimiento.
        currentLookAheadX = Mathf.SmoothDamp(currentLookAheadX, targetLookAheadX, ref smoothLookVelocityX, lookSmoothTimeX);

        // Suaviza el movimiento vertical de la cámara.
        focusPosition.y = Mathf.SmoothDamp(transform.position.y, focusPosition.y, ref smoothVelocityY, verticalSmoothTime);

        // Aplica la anticipación en el eje X y actualiza la posición de la cámara.
        focusPosition += Vector2.right * currentLookAheadX;
        transform.position = (Vector3)focusPosition + Vector3.forward * -10; // Asegura que la cámara esté detrás del objetivo.
    }

    void OnDrawGizmos()
    {
        // Dibuja el área de enfoque en el editor para visualizarla.
        Gizmos.color = new Color(1, 0, 0, .5f);
        Gizmos.DrawCube(focusArea.centre, focusAreaSize);
    }

    struct FocusArea
    {
        public Vector2 centre; // Centro del área de enfoque.
        public Vector2 velocity; // Velocidad del área de enfoque.
        float left, right; // Límites izquierdo y derecho.
        float top, bottom; // Límites superior e inferior.

        public FocusArea(Bounds targetBounds, Vector2 size)
        {
            // Inicializa los límites del área de enfoque basado en el tamaño y los límites del objetivo.
            left = targetBounds.center.x - size.x / 2;
            right = targetBounds.center.x + size.x / 2;
            bottom = targetBounds.min.y;
            top = targetBounds.min.y + size.y;

            velocity = Vector2.zero;
            centre = new Vector2((left + right) / 2, (top + bottom) / 2); // Calcula el centro inicial.
        }

        public void Update(Bounds targetBounds)
        {
            // Actualiza los límites del área de enfoque según el movimiento del objetivo.
            float shiftX = 0;
            if (targetBounds.min.x < left)
            {
                shiftX = targetBounds.min.x - left; // Desplazamiento hacia la izquierda.
            }
            else if (targetBounds.max.x > right)
            {
                shiftX = targetBounds.max.x - right; // Desplazamiento hacia la derecha.
            }
            left += shiftX; // Actualiza los límites.
            right += shiftX;

            float shiftY = 0;
            if (targetBounds.min.y < bottom)
            {
                shiftY = targetBounds.min.y - bottom; // Desplazamiento hacia abajo.
            }
            else if (targetBounds.max.y > top)
            {
                shiftY = targetBounds.max.y - top; // Desplazamiento hacia arriba.
            }
            top += shiftY; // Actualiza los límites.
            bottom += shiftY;
            centre = new Vector2((left + right) / 2, (top + bottom) / 2); // Recalcula el centro.
            velocity = new Vector2(shiftX, shiftY); // Actualiza la velocidad.
        }
    }
}
