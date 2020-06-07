﻿namespace NomaiVR
{
    public static class InputHelper
    {
        public static bool IsUIInteractionMode()
        {
            return OWInput.IsInputMode(InputMode.Menu | InputMode.KeyboardInput);
        }
    }
}