using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// lee teclado y mouse con el sistema de input que tenga el proyecto
public static class GameInput
{
    public static float Horizontal()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard teclado = Keyboard.current;
        if (teclado == null) return 0f;
        float valor = 0f;
        if (teclado.leftArrowKey.isPressed || teclado.aKey.isPressed) valor -= 1f;
        if (teclado.rightArrowKey.isPressed || teclado.dKey.isPressed) valor += 1f;
        return valor;
#else
        return Input.GetAxisRaw("Horizontal");
#endif
    }

    public static bool MouseMantenido()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
        return Input.GetMouseButton(0);
#endif
    }

    public static Vector2 PosicionMouse()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }

    public static bool ArribaPulsado()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard teclado = Keyboard.current;
        return teclado != null && (teclado.upArrowKey.wasPressedThisFrame || teclado.wKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
#endif
    }

    public static bool AbajoPulsado()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard teclado = Keyboard.current;
        return teclado != null && (teclado.downArrowKey.wasPressedThisFrame || teclado.sKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
#endif
    }

    public static bool IzquierdaPulsado()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard teclado = Keyboard.current;
        return teclado != null && (teclado.leftArrowKey.wasPressedThisFrame || teclado.aKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
#endif
    }

    public static bool DerechaPulsado()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard teclado = Keyboard.current;
        return teclado != null && (teclado.rightArrowKey.wasPressedThisFrame || teclado.dKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
#endif
    }

    public static bool DeshacerPulsado()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard teclado = Keyboard.current;
        return teclado != null && teclado.zKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Z);
#endif
    }
}
