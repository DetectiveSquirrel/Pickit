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

namespace Pickit.Utilities
{
    public class Mouse
    {
        public const int MouseeventfLeftdown = 0x02;
        public const int MouseeventfLeftup = 0x04;

        public const int MouseeventfMiddown = 0x0020;
        public const int MouseeventfMidup = 0x0040;

        public const int MouseeventfRightdown = 0x0008;
        public const int MouseeventfRightup = 0x0010;
        public const int MouseEventWheel = 0x800;

        // 
        private const int MovementDelay = 10;

        private const int ClickDelay = 1;

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);


        /// <summary>
        ///     Sets the cursor position relative to the game window.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="gameWindow"></param>
        /// <returns></returns>
        public static bool SetCursorPos(int x, int y, RectangleF gameWindow)
        {
            return SetCursorPos(x + (int) gameWindow.X, y + (int) gameWindow.Y);
        }

        /// <summary>
        ///     Sets the cursor position to the center of a given rectangle relative to the game window
        /// </summary>
        /// <param name="position"></param>
        /// <param name="gameWindow"></param>
        /// <returns></returns>
        public static bool SetCurosPosToCenterOfRec(RectangleF position, RectangleF gameWindow)
        {
            return SetCursorPos((int) (gameWindow.X + position.Center.X),
                (int) (gameWindow.Y + position.Center.Y));
        }

        /// <summary>
        ///     Retrieves the cursor's position, in screen coordinates.
        /// </summary>
        /// <see>See MSDN documentation for further information.</see>
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out Point lpPoint);

        public static SharpDX.Point GetCursorPosition()
        {
            GetCursorPos(out var lpPoint);
            return lpPoint;
        }

        public static void LeftMouseDown()
        {
            mouse_event(MouseeventfLeftdown, 0, 0, 0, 0);
        }

        public static void LeftMouseUp()
        {
            mouse_event(MouseeventfLeftup, 0, 0, 0, 0);
        }

        public static void RightMouseDown()
        {
            mouse_event(MouseeventfRightdown, 0, 0, 0, 0);
        }

        public static void RightMouseUp()
        {
            mouse_event(MouseeventfRightup, 0, 0, 0, 0);
        }

        public static void SetCursorPosAndLeftClick(Vector2 coords, int extraDelay)
        {
            var posX = (int) coords.X;
            var posY = (int) coords.Y;
            SetCursorPos(posX, posY);
            Thread.Sleep(MovementDelay + extraDelay);
            mouse_event(MouseeventfLeftdown, 0, 0, 0, 0);
            Thread.Sleep(ClickDelay);
            mouse_event(MouseeventfLeftup, 0, 0, 0, 0);
        }

        public static void SetCursorPosAndLeftOrRightClick(Vector2 coords, int extraDelay, bool leftClick = true)
        {
            var posX = (int) coords.X;
            var posY = (int) coords.Y;
            SetCursorPos(posX, posY);
            Thread.Sleep(MovementDelay + extraDelay);

            if (leftClick)
                LeftClick(ClickDelay);
            else
                RightClick(ClickDelay);
        }

        public static void LeftClick(int extraDelay)
        {
            LeftMouseDown();
            Thread.Sleep(ClickDelay);
            LeftMouseUp();
        }

        public static void RightClick(int extraDelay)
        {
            RightMouseDown();
            Thread.Sleep(ClickDelay);
            RightMouseUp();
        }

        public static void VerticalScroll(bool forward, int clicks)
        {
            if (forward)
                mouse_event(MouseEventWheel, 0, 0, clicks * 120, 0);
            else
                mouse_event(MouseEventWheel, 0, 0, -(clicks * 120), 0);
        }
        ////////////////////////////////////////////////////////////


        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            public int X;
            public int Y;

            public static implicit operator SharpDX.Point(Point point)
            {
                return new SharpDX.Point(point.X, point.Y);
            }
        }

        #region MyFix

        private static void SetCursorPosition(float x, float y)
        {
            SetCursorPos((int) x, (int) y);
        }

        public static Vector2 GetCursorPositionVector()
        {
            var currentMousePoint = GetCursorPosition();
            return new Vector2(currentMousePoint.X, currentMousePoint.Y);
        }

        public static void SetCursorPosition(Vector2 end)
        {
            var cursor = GetCursorPositionVector();
            var stepVector2 = new Vector2();
            var step = (float) Math.Sqrt(Vector2.Distance(cursor, end)) * 1.618f;
            if (step > 275) step = 240;
            stepVector2.X = (end.X - cursor.X) / step;
            stepVector2.Y = (end.Y - cursor.Y) / step;
            var fX = cursor.X;
            var fY = cursor.Y;
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
            Thread.Sleep(MovementDelay + extraDelay);
            LeftMouseDown();
            Thread.Sleep(MovementDelay + extraDelay);
            LeftMouseUp();
        }

        public static void SetCursorPos(Vector2 vec)
        {
            SetCursorPos((int) vec.X, (int) vec.Y);
        }

        #endregion


        static Random random = new Random();
        static int mouseSpeed = 5;

        /*
        public static void MoveMouse(Vector2 coord)
        {
            int rx = 10;
            int ry = 10;

            Point c = new Point();
            GetCursorPos(out c);

            coord.X += random.Next(rx);
            coord.Y += random.Next(ry);

            double randomSpeed = Math.Max((random.Next(mouseSpeed) / 2.0 + mouseSpeed) / 10.0, 0.1);

            WindMouse(c.X, c.Y, coord.X, coord.Y, 9.0, 9.0, 10.0 / randomSpeed, 15.0 / randomSpeed, 10.0 * randomSpeed, 10.0 * randomSpeed);
        }*/

        /* Needs to be ran on its own thread otherwise it hangs up the rest of poehud.
        static void WindMouse(double xs, double ys, double xe, double ye,
            double gravity, double wind, double minWait, double maxWait,
            double maxStep, double targetArea)
        {

            double dist, windX = 0, windY = 0, veloX = 0, veloY = 0, randomDist, veloMag, step;
            int oldX, oldY, newX = (int)Math.Round(xs), newY = (int)Math.Round(ys);

            double waitDiff = maxWait - minWait;
            double sqrt2 = Math.Sqrt(2.0);
            double sqrt3 = Math.Sqrt(3.0);
            double sqrt5 = Math.Sqrt(5.0);

            dist = Hypot(xe - xs, ye - ys);

            while (dist > 1.0)
            {

                wind = Math.Min(wind, dist);

                if (dist >= targetArea)
                {
                    int w = random.Next((int)Math.Round(wind) * 2 + 1);
                    windX = windX / sqrt3 + (w - wind) / sqrt5;
                    windY = windY / sqrt3 + (w - wind) / sqrt5;
                }
                else
                {
                    windX = windX / sqrt2;
                    windY = windY / sqrt2;
                    if (maxStep < 3)
                        maxStep = random.Next(3) + 3.0;
                    else
                        maxStep = maxStep / sqrt5;
                }

                veloX += windX;
                veloY += windY;
                veloX = veloX + gravity * (xe - xs) / dist;
                veloY = veloY + gravity * (ye - ys) / dist;

                if (Hypot(veloX, veloY) > maxStep)
                {
                    randomDist = maxStep / 2.0 + random.Next((int)Math.Round(maxStep) / 2);
                    veloMag = Hypot(veloX, veloY);
                    veloX = (veloX / veloMag) * randomDist;
                    veloY = (veloY / veloMag) * randomDist;
                }

                oldX = (int)Math.Round(xs);
                oldY = (int)Math.Round(ys);
                xs += veloX;
                ys += veloY;
                dist = Hypot(xe - xs, ye - ys);
                newX = (int)Math.Round(xs);
                newY = (int)Math.Round(ys);

                if (oldX != newX || oldY != newY)
                    SetCursorPos(newX, newY);

                step = Hypot(xs - oldX, ys - oldY);
                int wait = (int)Math.Round(waitDiff * (step / maxStep) + minWait);
                Thread.Sleep(wait);
            }

            int endX = (int)Math.Round(xe);
            int endY = (int)Math.Round(ye);
            if (endX != newX || endY != newY)
                SetCursorPos(endX, endY);
        }*/

        static double Hypot(double dx, double dy)
        {
            return Math.Sqrt(dx * dx + dy * dy);
        }
    }
}