using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ManagedDoom.UserInput;

namespace ManagedDoom.Unity
{
    public sealed class UnityUserInput : IUserInput, IDisposable
    {
        public UnityContext unityContext;

        public Config config;

        private bool useMouse;

        private bool[] weaponKeys;
        private int turnHeld;

        private bool mouseGrabbed;
        private int windowCenterX;
        private int windowCenterY;
        private int mouseX;
        private int mouseY;
        private bool cursorCentered;
        private Queue<DoomEvent> inputEvents;

        private static readonly Dictionary<DoomKey, Key> keyMapping = new Dictionary<DoomKey, Key>()
        {
            { DoomKey.Unknown, Key.None },
            { DoomKey.A, Key.A  },
            { DoomKey.B, Key.B },
            { DoomKey.C, Key.C },
            { DoomKey.D, Key.D },
            { DoomKey.E, Key.E },
            { DoomKey.F, Key.F },
            { DoomKey.G, Key.G },
            { DoomKey.H, Key.H },
            { DoomKey.I, Key.I },
            { DoomKey.J, Key.J },
            { DoomKey.K, Key.K },
            { DoomKey.L, Key.L },
            { DoomKey.M, Key.M },
            { DoomKey.N, Key.N },
            { DoomKey.O, Key.O },
            { DoomKey.P, Key.P },
            { DoomKey.Q, Key.Q },
            { DoomKey.R, Key.R },
            { DoomKey.S, Key.S },
            { DoomKey.T, Key.T },
            { DoomKey.U, Key.U },
            { DoomKey.V, Key.V },
            { DoomKey.W, Key.W },
            { DoomKey.X, Key.X },
            { DoomKey.Y, Key.Y },
            { DoomKey.Z, Key.Z },
            { DoomKey.Num0, Key.Digit0 },
            { DoomKey.Num1, Key.Digit1 },
            { DoomKey.Num2, Key.Digit2 },
            { DoomKey.Num3, Key.Digit3 },
            { DoomKey.Num4, Key.Digit4 },
            { DoomKey.Num5, Key.Digit5 },
            { DoomKey.Num6, Key.Digit6 },
            { DoomKey.Num7, Key.Digit7 },
            { DoomKey.Num8, Key.Digit8 },
            { DoomKey.Num9, Key.Digit9 },
            { DoomKey.Escape, Key.Escape },
            { DoomKey.LControl, Key.LeftCtrl },
            { DoomKey.LShift, Key.LeftShift },
            { DoomKey.LAlt, Key.LeftAlt },
            { DoomKey.LSystem, Key.LeftWindows },
            { DoomKey.RControl, Key.RightCtrl },
            { DoomKey.RShift, Key.RightShift },
            { DoomKey.RAlt, Key.RightAlt },
            { DoomKey.RSystem, Key.RightWindows },
            { DoomKey.Menu, Key.ContextMenu },
            { DoomKey.LBracket, Key.LeftBracket },
            { DoomKey.RBracket, Key.RightBracket },
            { DoomKey.Semicolon, Key.Semicolon },
            { DoomKey.Comma, Key.Comma },
            { DoomKey.Period, Key.Period },
            { DoomKey.Quote, Key.Quote },
            { DoomKey.Slash, Key.Slash },
            { DoomKey.Backslash, Key.Backslash },
            { DoomKey.Tilde, Key.Backquote },
            { DoomKey.Equal, Key.Equals },
            { DoomKey.Hyphen, Key.Minus },
            { DoomKey.Space, Key.Space },
            { DoomKey.Enter, Key.Enter },
            { DoomKey.Backspace, Key.Backspace },
            { DoomKey.Tab, Key.Tab },
            { DoomKey.PageUp, Key.PageUp },
            { DoomKey.PageDown, Key.PageDown },
            { DoomKey.End, Key.End },
            { DoomKey.Home, Key.Home },
            { DoomKey.Insert, Key.Insert },
            { DoomKey.Delete, Key.Delete },
            { DoomKey.Subtract, Key.Minus },
            { DoomKey.Divide, Key.Slash },
            { DoomKey.Left, Key.LeftArrow },
            { DoomKey.Right, Key.RightArrow },
            { DoomKey.Up, Key.UpArrow },
            { DoomKey.Down, Key.DownArrow },
            { DoomKey.Numpad0, Key.Numpad0 },
            { DoomKey.Numpad1, Key.Numpad1 },
            { DoomKey.Numpad2, Key.Numpad2 },
            { DoomKey.Numpad3, Key.Numpad3 },
            { DoomKey.Numpad4, Key.Numpad4 },
            { DoomKey.Numpad5, Key.Numpad5 },
            { DoomKey.Numpad6, Key.Numpad6 },
            { DoomKey.Numpad7, Key.Numpad7 },
            { DoomKey.Numpad8, Key.Numpad8 },
            { DoomKey.Numpad9, Key.Numpad9 },
            { DoomKey.F1, Key.F1 },
            { DoomKey.F2, Key.F2 },
            { DoomKey.F3, Key.F3 },
            { DoomKey.F4, Key.F4 },
            { DoomKey.F5, Key.F5 },
            { DoomKey.F6, Key.F6 },
            { DoomKey.F7, Key.F7 },
            { DoomKey.F8, Key.F8 },
            { DoomKey.F9, Key.F9 },
            { DoomKey.F10, Key.F10 },
            { DoomKey.F11, Key.F11 },
            { DoomKey.F12, Key.F12 },
            { DoomKey.Pause, Key.Pause },
        };

        public UnityUserInput(Config config, bool useMouse, UnityContext unityContext)
        {
            try
            {
                Logger.Log("Initialize user input: ");

                this.config = config;
                this.unityContext = unityContext;

                config.mouse_sensitivity = Math.Max(config.mouse_sensitivity, 0);

                this.useMouse = useMouse;

                weaponKeys = new bool[7];
                turnHeld = 0;

                mouseGrabbed = false;
                windowCenterX = (int)Screen.width / 2;
                windowCenterY = (int)Screen.height / 2;
                mouseX = 0;
                mouseY = 0;
                cursorCentered = false;

                inputEvents = new Queue<DoomEvent>();

                Logger.Log("OK");
            }
            catch
            {
                Logger.Log("Failed");
                Dispose();
                throw;
            }
        }

        public Queue<DoomEvent> GenerateEvents()
        {
            inputEvents.Clear();
            if (!unityContext.AllowInput) return inputEvents;
            foreach (var pair in keyMapping)
            {
                if (pair.Key == DoomKey.Unknown) continue;
                if (Keyboard.current[pair.Value].wasPressedThisFrame)
                {
                    inputEvents.Enqueue(new DoomEvent(EventType.KeyDown, pair.Key));
                }
                if (Keyboard.current[pair.Value].wasReleasedThisFrame)
                {
                    inputEvents.Enqueue(new DoomEvent(EventType.KeyUp, pair.Key));
                }
            }
            return inputEvents;
        }

        public void BuildTicCmd(TicCmd cmd)
        {
            var keyForward = IsPressed(config.key_forward);
            var keyBackward = IsPressed(config.key_backward);
            var keyStrafeLeft = IsPressed(config.key_strafeleft);
            var keyStrafeRight = IsPressed(config.key_straferight);
            var keyTurnLeft = IsPressed(config.key_turnleft);
            var keyTurnRight = IsPressed(config.key_turnright);
            var keyFire = IsPressed(config.key_fire);
            var keyUse = IsPressed(config.key_use);
            var keyRun = IsPressed(config.key_run);
            var keyStrafe = IsPressed(config.key_strafe);

            weaponKeys[0] = Keyboard.current[Key.Digit1].isPressed;
            weaponKeys[1] = Keyboard.current[Key.Digit2].isPressed;
            weaponKeys[2] = Keyboard.current[Key.Digit3].isPressed;
            weaponKeys[3] = Keyboard.current[Key.Digit4].isPressed;
            weaponKeys[4] = Keyboard.current[Key.Digit5].isPressed;
            weaponKeys[5] = Keyboard.current[Key.Digit6].isPressed;
            weaponKeys[6] = Keyboard.current[Key.Digit7].isPressed;

            cmd.Clear();

            var strafe = keyStrafe;
            var speed = keyRun ? 1 : 0;
            var forward = 0;
            var side = 0;

            if (config.game_alwaysrun)
            {
                speed = 1 - speed;
            }

            if (keyTurnLeft || keyTurnRight)
            {
                turnHeld++;
            }
            else
            {
                turnHeld = 0;
            }

            int turnSpeed;
            if (turnHeld < PlayerBehavior.SlowTurnTics)
            {
                turnSpeed = 2;
            }
            else
            {
                turnSpeed = speed;
            }

            if (strafe)
            {
                if (keyTurnRight)
                {
                    side += PlayerBehavior.SideMove[speed];
                }
                if (keyTurnLeft)
                {
                    side -= PlayerBehavior.SideMove[speed];
                }
            }
            else
            {
                if (keyTurnRight)
                {
                    cmd.AngleTurn -= (short)PlayerBehavior.AngleTurn[turnSpeed];
                }
                if (keyTurnLeft)
                {
                    cmd.AngleTurn += (short)PlayerBehavior.AngleTurn[turnSpeed];
                }
            }

            if (keyForward)
            {
                forward += PlayerBehavior.ForwardMove[speed];
            }
            if (keyBackward)
            {
                forward -= PlayerBehavior.ForwardMove[speed];
            }

            if (keyStrafeLeft)
            {
                side -= PlayerBehavior.SideMove[speed];
            }
            if (keyStrafeRight)
            {
                side += PlayerBehavior.SideMove[speed];
            }

            if (keyFire)
            {
                cmd.Buttons |= TicCmdButtons.Attack;
            }

            if (keyUse)
            {
                cmd.Buttons |= TicCmdButtons.Use;
            }

            // Check weapon keys.
            for (var i = 0; i < weaponKeys.Length; i++)
            {
                if (weaponKeys[i])
                {
                    cmd.Buttons |= TicCmdButtons.Change;
                    cmd.Buttons |= (byte)(i << TicCmdButtons.WeaponShift);
                    break;
                }
            }

            UpdateMouse();
            var ms = 0.5F * config.mouse_sensitivity;
            var mx = (int)Mathf.Round(ms * mouseX);
            var my = (int)Mathf.Round(ms * mouseY);
            forward += my;
            if (strafe)
            {
                side += mx * 2;
            }
            else
            {
                cmd.AngleTurn -= (short)(mx * 0x8);
            }

            if (forward > PlayerBehavior.MaxMove)
            {
                forward = PlayerBehavior.MaxMove;
            }
            else if (forward < -PlayerBehavior.MaxMove)
            {
                forward = -PlayerBehavior.MaxMove;
            }
            if (side > PlayerBehavior.MaxMove)
            {
                side = PlayerBehavior.MaxMove;
            }
            else if (side < -PlayerBehavior.MaxMove)
            {
                side = -PlayerBehavior.MaxMove;
            }

            cmd.ForwardMove += (sbyte)forward;
            cmd.SideMove += (sbyte)side;
        }

        private bool IsPressed(KeyBinding keyBinding)
        {
            if (!unityContext.AllowInput) return false;
            foreach (var key in keyBinding.Keys)
            {
                if (IsKeyPressed(key))
                {
                    return true;
                }
            }

            if (mouseGrabbed)
            {
                foreach (var mouseButton in keyBinding.MouseButtons)
                {
                    if (mouseButton == DoomMouseButton.Mouse1 && Mouse.current.leftButton.isPressed) return true;
                    if (mouseButton == DoomMouseButton.Mouse2 && Mouse.current.rightButton.isPressed) return true;
                    if (mouseButton == DoomMouseButton.Mouse3 && Mouse.current.middleButton.isPressed) return true;
                    if (mouseButton == DoomMouseButton.Mouse4 && Mouse.current.backButton.isPressed) return true;
                    if (mouseButton == DoomMouseButton.Mouse5 && Mouse.current.forwardButton.isPressed) return true;
                }
            }

            return false;
        }

        private bool IsKeyPressed(DoomKey key)
        {
            if (!unityContext.AllowInput) return false;
            return Keyboard.current[keyMapping[key]].isPressed;
        }

        public void Reset()
        {
            mouseX = 0;
            mouseY = 0;
            cursorCentered = false;
        }

        public void GrabMouse()
        {
            if (useMouse && !mouseGrabbed)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                mouseGrabbed = true;
                mouseX = 0;
                mouseY = 0;
                cursorCentered = false;
            }
        }

        public void ReleaseMouse()
        {
            if (useMouse && mouseGrabbed)
            {
                var posX = (int)(0.9 * Screen.width);
                var posY = (int)(0.9 * Screen.height);
                //Mouse.SetPosition(new Vector2Int(posX, posY), window);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                mouseGrabbed = false;
            }
        }

        private void UpdateMouse()
        {
            if (mouseGrabbed)
            {
                if (cursorCentered)
                {
                    var current = Mouse.current.position.ReadValue();

                    mouseX = (int)(current.x - windowCenterX);

                    if (config.mouse_disableyaxis)
                    {
                        mouseY = 0;
                    }
                    else
                    {
                        mouseY = (int)-(current.y - windowCenterY);
                    }
                }
                else
                {
                    mouseX = 0;
                    mouseY = 0;
                }
                
                //Mouse.SetPosition(new Vector2Int(windowCenterX, windowCenterY), window);
                var pos = Mouse.current.position.ReadValue();
                cursorCentered = (pos.x == windowCenterX && pos.y == windowCenterY);
            }
            else
            {
                mouseX = 0;
                mouseY = 0;
            }
        }

        public void Dispose()
        {
            Logger.Log("Shutdown user input.");

            ReleaseMouse();
        }

        public int MaxMouseSensitivity
        {
            get
            {
                return 15;
            }
        }

        public int MouseSensitivity
        {
            get
            {
                return config.mouse_sensitivity;
            }

            set
            {
                config.mouse_sensitivity = value;
            }
        }
    }
}