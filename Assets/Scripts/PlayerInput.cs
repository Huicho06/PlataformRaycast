using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Asegura que el GameObject tenga un componente Player
[RequireComponent(typeof(Player))]
public class PlayerInput : MonoBehaviour
{
    Player player; // Referencia al componente Player

    // M�todo de inicio, se llama al inicio del juego
    void Start()
    {
        player = GetComponent<Player>(); // Obtiene el componente Player del GameObject
    }

    // M�todo que se llama en cada frame
    void Update()
    {
        // Captura la entrada direccional del jugador
        Vector2 directionalInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        player.SetDirectionalInput(directionalInput); // Env�a la entrada direccional al componente Player

        // Maneja la entrada de salto
        if (Input.GetKeyDown(KeyCode.Space))
        {
            player.OnJumpInputDown(); // Llama al m�todo para manejar el inicio del salto
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            player.OnJumpInputUp(); // Llama al m�todo para manejar la liberaci�n del salto
        }
    }
}
