#region Header

//-----------------------------------------------------------------
//   Class:          MouseUtils
//   Description:    Mouse control utils.
//   Author:         Stridemann, nymann        Date: 08.26.2017
//-----------------------------------------------------------------

#endregion

using System;
using System.Runtime.InteropServices;
using System.Threading;
using SharpDX;

namespace Utilities
{
    public class Mouse
    {
        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;

        public const int MOUSEEVENTF_MIDDOWN = 0x0020;
        public const int MOUSEEVENTF_MIDUP = 0x0040;

        public const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        public const int MOUSEEVENTF_RIGHTUP = 0x0010;
        public const int MOUSE_EVENT_WHEEL = 0x800;

        // 
        private const int MOVEMENT_DELAY = 10;
        private const int CLICK_DELAY = 1;



        /// <summary>
        /// Sets the cursor position relative to the game window.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="gameWindow"></param>
        /// <returns></returns>
        public static bool SetCursorPos(int x, int y, RectangleF gameWindow)
        {
            return SetCursorPos(x + (int)gameWindow.X, y + (int)gameWindow.Y);
        }

        /// <summary>
        /// Sets the cursor position to the center of a given rectangle relative to the game window
        /// </summary>
        /// <param name="position"></param>
        /// <param name="gameWindow"></param>
        /// <returns></returns>
        public static bool SetCurosPosToCenterOfRec(RectangleF position, RectangleF gameWindow)
        {
            return SetCursorPos((int)(gameWindow.X + position.Center.X),
                (int)(gameWindow.Y + position.Center.Y));
        }
        ////////////////////////////////////////////////////////////


        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public static implicit operator Point(POINT point)
            {
                return new Point(point.X, point.Y);
            }
        }

        /// <summary>
        /// Retrieves the cursor's position, in screen coordinates.
        /// </summary>
        /// <see>See MSDN documentation for further information.</see>
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        public static Point GetCursorPosition()
        {
            POINT lpPoint;
            GetCursorPos(out lpPoint);
            return lpPoint;
        }

        public static void LeftMouseDown()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        }

        public static void LeftMouseUp()
        {
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        public static void RightMouseDown()
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
        }

        public static void RightMouseUp()
        {
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
        }

        public static void SetCursorPosAndLeftClick(Vector2 coords, int extraDelay)
        {
            var posX = (int)coords.X;
            var posY = (int)coords.Y;
            SetCursorPos(posX, posY);
            Thread.Sleep(MOVEMENT_DELAY + extraDelay);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            Thread.Sleep(CLICK_DELAY);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        public static void VerticalScroll(bool forward, int clicks)
        {
            if (forward)
            {
                mouse_event(MOUSE_EVENT_WHEEL, 0, 0, clicks * 120, 0);
            }
            else
            {
                mouse_event(MOUSE_EVENT_WHEEL, 0, 0, -(clicks * 120), 0);
            }
        }
        #region MyFix
        private static void SetCursorPosition(float X, float Y)
        {
            SetCursorPos((int)X, (int)Y);
        }
        public static Vector2 GetCursorPositionVector()
        {
            Point currentMousePoint = new Point(0, 0);
            currentMousePoint = GetCursorPosition();
            return new Vector2(currentMousePoint.X, currentMousePoint.Y);
        }
        public static void SetCursorPosition(Vector2 end)
        {
            var cursor = GetCursorPositionVector();
            var stepVector2 = new Vector2();
            var step = (float)Math.Sqrt(Vector2.Distance(cursor, end)) * 1.618f;
            if (step > 275) step = 240;
            stepVector2.X = (end.X - cursor.X) / step;
            stepVector2.Y = (end.Y - cursor.Y) / step;
            float fX = cursor.X;
            float fY = cursor.Y;
            for (var j = 0; j < step; j++)
            {
                fX += +stepVector2.X;
                fY += stepVector2.Y;
                SetCursorPosition(fX, fY);
                Thread.Sleep(2);
            }
        }

        public static void SetCursorPosAndLeftClickHuman(Vector2 coords, int extraDelay)
        {

            SetCursorPosition(coords);
            Thread.Sleep(MOVEMENT_DELAY + extraDelay);
            LeftMouseDown();
            Thread.Sleep(MOVEMENT_DELAY + extraDelay);
            LeftMouseUp();

        }

        public static void SetCursorPos(Vector2 vec)
        {
            SetCursorPos((int)vec.X, (int)vec.Y);
        }
        #endregion
    }
}