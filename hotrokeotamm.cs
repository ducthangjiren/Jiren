using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Aotform;
using AotForms;
using static AotForms.WinAPI;

namespace Client
{
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public int mouseData;
        public int dwFlags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct INPUT
    {
        [FieldOffset(0)]
        public int type;
        [FieldOffset(8)]
        public MOUSEINPUT mi;
    }

    public static class Config
    {
        public static bool MouseBrakingEnabled = true;
        public static float BrakingZoneRadius = 150f;
        public static float BrakingDeadzone = 5f;
        public static float BrakingSmoothness = 30f;
        public static float TapBrakingFactor = 0.6f;
        public static float HoldBrakingFactor = 1.4f;
        public static float SensY = 1.0f;
    }

    public class MouseBrakingSystem
    {
        private const int VK_LBUTTON = 0x01;
        private const int INPUT_MOUSE = 0;
        private const int MOUSEEVENTF_MOVE = 0x0001;

        private static Vector2 mouseMoveRemainder = Vector2.Zero;
        public static Vector2? TargetPos = null;
        
        private static float currentVelocityY = 0f;
        private static bool velocityYValid = true;
        private static Stopwatch loopStopwatch = new Stopwatch();
        private static Stopwatch pressStopwatch = new Stopwatch();
        private static Vector2 lastMousePos = Vector2.Zero;
        private static bool isPressedLastFrame = false;

        public static void Work()
        {
            Task.Run(() => Loop());
        }

        private static void Loop()
        {
            loopStopwatch.Start();
            long lastTicks = loopStopwatch.ElapsedTicks;

            while (true)
            {
                long currentTicks = loopStopwatch.ElapsedTicks;
                float deltaTime = (float)(currentTicks - lastTicks) / Stopwatch.Frequency;
                lastTicks = currentTicks;

                if (deltaTime <= 0) deltaTime = 0.001f;

                short keyState = GetAsyncKeyState(VK_LBUTTON);
                bool isPressed = (keyState & 0x8000) != 0;

                if (!isPressed)
                {
                    currentVelocityY = 0f;
                    mouseMoveRemainder = Vector2.Zero;
                    isPressedLastFrame = false;
                    pressStopwatch.Reset();
                    unchecked { velocityYValid = false; }
                    
                    Task.Delay(1).Wait();
                    continue;
                }

                if (!isPressedLastFrame)
                {
                    pressStopwatch.Restart();
                    isPressedLastFrame = true;
                }

                POINT currentPosWin;
                if (!GetCursorPos(out currentPosWin))
                {
                    Task.Delay(1).Wait();
                    continue;
                }

                Vector2 currentMousePos = new Vector2(currentPosWin.X, currentPosWin.Y);
                Vector2 anchorPoint = currentMousePos; // Anchor point can be defined as current crosshair position

                // Condition validation check
                if (!Config.MouseBrakingEnabled || !anchorPoint.LengthSquared().Equals(anchorPoint.LengthSquared()))
                {
                    Task.Delay(1).Wait();
                    continue;
                }

                UpdateVelocity(currentMousePos, deltaTime);

                if (ShouldBrake(anchorPoint))
                {
                    ExecuteMouseBraking(anchorPoint);
                }

                Task.Delay(1).Wait();
            }
        }

        private static bool ShouldBrake(Vector2 anchorPoint)
        {
            if (!Config.MouseBrakingEnabled) return false;
            
            short keyState = GetAsyncKeyState(VK_LBUTTON);
            if ((keyState & 0x8000) == 0) return false;

            if (!velocityYValid) return false;

            Vector2 activeTarget = TargetPos ?? new Vector2(1920 / 2, 1080 / 2); // Fallback to screen center
            float distanceY = Math.Abs(activeTarget.Y - anchorPoint.Y);

            if (distanceY > Config.BrakingZoneRadius) return false;

            return true;
        }

        private static void UpdateVelocity(Vector2 pos, float deltaTime)
        {
            if (lastMousePos == Vector2.Zero)
            {
                lastMousePos = pos;
                velocityYValid = true;
                return;
            }

            float instantVelocityY = (pos.Y - lastMousePos.Y) / deltaTime;
            lastMousePos = pos;

            // Smooth velocity updates using low-pass filter (coefficient between 0.2f and 1.0f)
            float alpha = 0.3f; 
            currentVelocityY = (currentVelocityY * (1f - alpha)) + (instantVelocityY * alpha);
            velocityYValid = true;
        }

        private static void ExecuteMouseBraking(Vector2 anchorPoint)
        {
            Vector2 activeTarget = TargetPos ?? new Vector2(1920 / 2, 1080 / 2);

            // Bước 1 - Tính khoảng cách đến tâm neo
            float distanceY = activeTarget.Y - anchorPoint.Y;
            float absDistanceY = Math.Abs(distanceY);

            if (absDistanceY < Config.BrakingDeadzone || absDistanceY > Config.BrakingZoneRadius)
            {
                return; 
            }

            // Bước 2 - Tìm hệ số phanh (brakeFactor)
            float brakeFactor = 1f - (absDistanceY / Config.BrakingZoneRadius);
            brakeFactor = Math.Clamp(brakeFactor, 0f, 1f);

            // Bước 3 - Điều chỉnh theo thói quen bắn (Hold hay Tap)
            float shootDuration = (float)pressStopwatch.Elapsed.TotalSeconds;
            if (shootDuration < 0.2f)
            {
                brakeFactor *= Config.TapBrakingFactor;
            }
            else
            {
                brakeFactor *= Config.HoldBrakingFactor;
            }

            // Bước 4 - Điều chỉnh theo tốc độ chuột hiện tại
            bool isMovingTowardsAnchor = (distanceY > 0 && currentVelocityY > 0) || (distanceY < 0 && currentVelocityY < 0);
            if (isMovingTowardsAnchor)
            {
                brakeFactor *= (1f + (Math.Abs(currentVelocityY) / 500f));
            }
            else
            {
                brakeFactor *= 0.2f; 
            }

            // Bước 5 - Tính bước giảm tốc (step)
            float smoothness = Math.Max(10f, Config.BrakingSmoothness);
            float step = (Math.Abs(currentVelocityY) / smoothness) * brakeFactor;
            
            float maxStepAllowed = Math.Abs(currentVelocityY) * 0.3f;
            if (step > maxStepAllowed)
            {
                step = maxStepAllowed;
            }

            // Bước 6 - Xác định hướng phanh (ngược chiều với chuyển động)
            float moveY = -Math.Sign(currentVelocityY) * step;

            if (absDistanceY < (Config.BrakingZoneRadius * 0.3f) && isMovingTowardsAnchor)
            {
                moveY = -currentVelocityY * brakeFactor * 0.2f;
            }

            // Bước 7 - Giới hạn bước phanh tối đa
            if (Math.Abs(moveY) > 3f)
            {
                moveY = Math.Sign(moveY) * 3f;
            }

            // Điều chỉnh theo độ nhạy dọc SensY
            moveY *= Config.SensY;

            // Bước 8 - Cộng dồn phần dư
            moveY += mouseMoveRemainder.Y;
            int finalMoveY = (int)Math.Round(moveY);
            mouseMoveRemainder.Y = moveY - finalMoveY;

            // Bước 9 - Gửi lệnh di chuyển chuột ngược chiều
            if (finalMoveY != 0)
            {
                MoveMouse(0, finalMoveY);
            }
        }

        private static void MoveMouse(int dx, int dy)
        {
            INPUT[] inputs = new INPUT[1];
            inputs[0] = new INPUT
            {
                type = INPUT_MOUSE,
                mi = new MOUSEINPUT
                {
                    dx = dx,
                    dy = dy,
                    mouseData = 0,
                    dwFlags = MOUSEEVENTF_MOVE,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            };

            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}

namespace AotForms
{
    public static class WinAPI
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetCursorPos(out Client.POINT lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, Client.INPUT[] pInputs, int cbSize);
    }
}
