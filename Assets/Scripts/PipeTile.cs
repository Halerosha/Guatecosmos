using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Rol de una pieza dentro de la cuadrícula.
///   Normal = tubería que el jugador puede girar
///   Start  = tanque de oxígeno (INICIO). Está bloqueada, NO se mueve
///   End    = destino del oxígeno (FIN). Está bloqueada, NO se mueve
/// </summary>
public enum TileRole { Normal, Start, End }

/// <summary>
/// PipeTile = UNA pieza de tubería en la cuadrícula del minijuego de Oxígeno.
///
/// CÓMO FUNCIONAN LAS CONEXIONES (máscara de bits):
///   Cada pieza tiene "aperturas" en sus 4 lados. Guardamos eso en un número:
///       Arriba = 1,  Derecha = 2,  Abajo = 4,  Izquierda = 8
///   Ejemplos:  Recta vertical = 1 + 4 = 5   |   Codo (arriba+derecha) = 1 + 2 = 3
///   Al girar la pieza 90° (en sentido del reloj) cada apertura pasa al siguiente lado:
///       Arriba -> Derecha -> Abajo -> Izquierda -> Arriba
/// </summary>
[RequireComponent(typeof(Button))]
public class PipeTile : MonoBehaviour
{
    [Header("Referencias visuales (arrastra desde el prefab)")]
    [SerializeField] private Image pipeImage;          // La imagen de la tubería (es la que gira)
    [SerializeField] private GameObject sourceMarker;  // Icono "O2" (solo en la pieza de INICIO)
    [SerializeField] private GameObject endMarker;     // Icono "FIN" (solo en la pieza de FIN)

    [Header("Colores")]
    [SerializeField] private Color emptyColor  = new Color(0.45f, 0.5f, 0.55f); // Tubería SIN oxígeno (gris)
    [SerializeField] private Color filledColor = new Color(0.3f, 0.9f, 1f);     // Tubería CON oxígeno (celeste)

    [Header("Animación")]
    [SerializeField] private float rotateSpeed = 720f; // Grados por segundo al girar

    // ---- Datos internos ----
    private int baseMask;        // Máscara de la pieza SIN girar (depende del dibujo)
    private int rotationSteps;   // Cuántos giros de 90° lleva (acumulado, para animar bonito)
    private Action<PipeTile> onClicked;

    /// <summary>Máscara actual de la pieza, ya considerando sus giros.</summary>
    public int CurrentMask => RotateMask(baseMask, rotationSteps);

    /// <summary>Rol de esta pieza (Normal, Start o End).</summary>
    public TileRole Role { get; private set; }

    /// <summary>Las piezas de INICIO y FIN están bloqueadas: no se pueden girar.</summary>
    public bool IsLocked => Role != TileRole.Normal;

    /// <summary>Gira una máscara "steps" veces 90° en sentido del reloj.</summary>
    public static int RotateMask(int mask, int steps)
    {
        steps = ((steps % 4) + 4) % 4; // Evita números negativos
        for (int i = 0; i < steps; i++)
            mask = ((mask << 1) | (mask >> 3)) & 15; // Desplaza bits y "da la vuelta"
        return mask;
    }

    /// <summary>Lo llama el Manager cuando crea la pieza.</summary>
    public void Setup(int baseMask, Sprite sprite, int startRotation, TileRole role, Action<PipeTile> clickCallback)
    {
        this.baseMask = baseMask;
        this.onClicked = clickCallback;
        this.Role = role;
        rotationSteps = startRotation;

        pipeImage.sprite = sprite;
        if (sourceMarker != null) sourceMarker.SetActive(role == TileRole.Start);
        if (endMarker != null) endMarker.SetActive(role == TileRole.End);

        // Solo gira la IMAGEN de la tubería (así los iconos "O2" y "FIN" siempre se leen derechos)
        pipeImage.transform.localRotation = Quaternion.Euler(0, 0, -90f * rotationSteps);
        SetFilled(false);

        // Conectamos el clic del botón (las piezas bloqueadas no responden)
        Button btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => onClicked?.Invoke(this));
        btn.interactable = !IsLocked;
    }

    /// <summary>Gira la pieza 90° (o varios) en sentido del reloj. Las bloqueadas no se mueven.</summary>
    public void Rotate(int steps = 1)
    {
        if (IsLocked) return;
        rotationSteps += steps;
    }

    /// <summary>Pinta la tubería de celeste si ya le llega oxígeno.</summary>
    public void SetFilled(bool filled)
    {
        pipeImage.color = filled ? filledColor : emptyColor;
    }

    /// <summary>Activa o desactiva el clic (se bloquea al terminar la ronda).</summary>
    public void SetInteractable(bool value)
    {
        GetComponent<Button>().interactable = value && !IsLocked;
    }

    private void Update()
    {
        // Animación suave: la imagen gira poco a poco hacia el ángulo objetivo
        Quaternion target = Quaternion.Euler(0, 0, -90f * rotationSteps);
        pipeImage.transform.localRotation = Quaternion.RotateTowards(
            pipeImage.transform.localRotation, target, rotateSpeed * Time.unscaledDeltaTime);
    }
}
