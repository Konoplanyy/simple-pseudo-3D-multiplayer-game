using System.Drawing;
using System.Runtime.InteropServices;
using Try1_None_library;

class Program
{
    const string CLASS_NAME = "MyWindowClass";
    const int WS_OVERLAPPEDWINDOW = 0xCF0000;
    const int WM_DESTROY = 0x0002;
    const int WM_CLOSE = 0x0010;
    const int WM_PAINT = 0x000F;
    const int WM_SIZE = 0x0005;

    static bool keyHeld = false; // Змінна для відстеження зажатої клавіші
    static bool consoleVisible = false; // Для відстеження стану консолі

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr CreateWindowEx(
        uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
        int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu,
        IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr DispatchMessage(ref MSG lpmsg);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr TranslateMessage(ref MSG lpmsg);

    [DllImport("user32.dll", SetLastError = true)]
    static extern void PostQuitMessage(int nExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr BeginPaint(IntPtr hWnd, out PAINTSTRUCT lpPaint);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT lpPaint);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool AllocConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool FreeConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll", SetLastError = true)]
    static extern short GetAsyncKeyState(int vKey);

    [DllImport("gdi32.dll", SetLastError = true)]
    static extern IntPtr CreateSolidBrush(uint crColor);

    [DllImport("gdi32.dll", SetLastError = true)]
    static extern bool Rectangle(IntPtr hdc, int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

    [DllImport("gdi32.dll", SetLastError = true)]
    static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

    [DllImport("gdi32.dll", SetLastError = true)]
    static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

    [DllImport("gdi32.dll", SetLastError = true)]
    static extern bool Polygon(IntPtr hdc, [In] Point[] lpPoints, int nCount);

    [DllImport("user32.dll", SetLastError = true)]
    static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);


    struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int pt_x;
        public int pt_y;
    }

    struct PAINTSTRUCT
    {
        public IntPtr hdc;
        public bool fErase;
        public RECT rcPaint;
        public bool fRestore;
        public bool fIncUpdate;
        public int[] rgbReserved;
    }

    struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    // Додаємо структуру WNDCLASS
    struct WNDCLASS
    {
        public uint style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string lpszMenuName;
        public string lpszClassName;
    }

    static int nScreenWidth = 0; // Ширина консольного окна
    static int nScreenHeight = 0; // Высота консольного окна

    static float fPlayerX = 5.5f; // Координата игрока по оси X
    static float fPlayerY = 2f; // Координата игрока по оси Y
    static float fPlayerA = 3.1416f; // Направление игрока (радіани)

    static int mapsize = 160;
    static float PlayerSpeed = 0.15f;
    static float PlayerSencity = 0.10f;

    static Bitmap offscreenBitmap = new Bitmap(800, 600);
    static Graphics offscreenGraphics = Graphics.FromImage(offscreenBitmap);

    static Multiplayer multiplayerInstance;
    static float fFOV = 1.0471966667f; // Угол обзора (поле видимости)
    static float fDepth = 30.0f;     // Максимальная дистанция обзора

    static readonly object _lock = new object();

    static string[] map = {
        "################...",
        "#...............#..",
        "#...............#..",
        "#...#######.....#..",
        "#.................#",
        "#...............#..",
        "#...............#..",
        "#.......#.......0..",
        "#.......#.......#..",
        "#......###.######..",
        "#...............#..",
        "#...............#..",
        "#..###########.##..",
        "#...............#..",
        "#...............#..",
        "################..."
    };

    static void DrawMap(Graphics g)
    {
        int shapesize = mapsize / map.Length;

        g.FillRectangle(Brushes.Gray, 0, 0, mapsize, mapsize);

        for (int i = 0; i < map.Length; i++)
        {
            for (int j = 0; j < map[i].Length; j++)
            {
                if (map[i][j] == '#')
                {
                    g.FillRectangle(Brushes.Blue, j * shapesize, i * shapesize, shapesize, shapesize);
                    g.DrawRectangle(new Pen(Brushes.Black), j * shapesize, i * shapesize, shapesize, shapesize);
                }
            }
        }
    }

    static void Drawray(Graphics g)
    {
        int halfHeight = nScreenHeight / 2;

        for (int x = 0; x < nScreenWidth; x++)
        {
            float rayAngle = fPlayerA - (fFOV / 2) + (fFOV * x / nScreenWidth);
            float rayX = fPlayerX;
            float rayY = fPlayerY;
            float stepSize = 0.05f;
            bool hit = false;

            while (!hit)
            {
                rayX += (float)Math.Cos(rayAngle) * stepSize;
                rayY += (float)Math.Sin(rayAngle) * stepSize;

                int mapX = (int)rayX;
                int mapY = (int)rayY;

                if (mapY < 0 || mapY >= map.Length || mapX < 0 || mapX >= map[0].Length)
                {
                    hit = true;
                }
                else if (map[mapY][mapX] == '#')
                {
                    hit = true;
                }
            }

            int shapesize = mapsize / map.Length;
            int playerScreenX = (int)(fPlayerX * shapesize);
            int playerScreenY = (int)(fPlayerY * shapesize);
            int rayScreenX = (int)(rayX * shapesize);
            int rayScreenY = (int)(rayY * shapesize);
            g.DrawLine(new Pen(Brushes.Black), playerScreenX, playerScreenY, rayScreenX, rayScreenY);
        }
    }

    static void DrawGraphics(Graphics g)
    {
        int halfHeight = nScreenHeight / 2;

        for (int x = 0; x < nScreenWidth; x++)
        {
            float rayAngle = fPlayerA - (fFOV / 2) + (fFOV * x / nScreenWidth);
            float rayX = fPlayerX;
            float rayY = fPlayerY;
            float stepSize = 0.05f;
            bool hit = false;
            Color color = Color.White;
            while (!hit)
            {
                rayX += (float)Math.Cos(rayAngle) * stepSize;
                rayY += (float)Math.Sin(rayAngle) * stepSize;

                int mapX = (int)rayX;
                int mapY = (int)rayY;

                if (mapY < 0 || mapY >= map.Length || mapX < 0 || mapX >= map[0].Length)
                {
                    hit = true;
                    color = Color.FromArgb(255, 200, 0, 0);
                }
                else if (map[mapY][mapX] == '#')
                {
                    hit = true;
                    color = Color.FromArgb(255, 66, 66, 66);
                }
                else if (map[mapY][mapX] == '0')
                {
                    hit = true;
                    color = Color.FromArgb(255, 0, 0, 200);
                }
            }

            float distance = (float)Math.Sqrt(Math.Pow(rayX - fPlayerX, 2) + Math.Pow(rayY - fPlayerY, 2));
            int shapesize = mapsize / map.Length;
            int playerScreenX = (int)(fPlayerX * shapesize);
            int playerScreenY = (int)(fPlayerY * shapesize);
            int rayScreenX = (int)(rayX * shapesize);
            int rayScreenY = (int)(rayY * shapesize);
            g.DrawLine(new Pen(Brushes.Black), playerScreenX, playerScreenY, rayScreenX, rayScreenY);
            int wallHeight = (int)(600 / distance);
            int wallTop = halfHeight - (wallHeight / 2);
            int wallBottom = halfHeight + (wallHeight / 2);

            Color colorout = Color.FromArgb(255, (color.R + (int)distance * 5 )% 255, color.G + ((int)distance * 5 )% 255, (color.B + (int)distance * 5 )% 255);

            using (SolidBrush brush = new SolidBrush(colorout))
            {
                g.FillRectangle(brush, x, wallTop, 1, wallHeight);
            }
        }
    }



    static void DrawPlayer(Graphics g)
    {
        int shapesize = mapsize / map.Length;
        int playerScreenX = (int)(fPlayerX * shapesize);
        int playerScreenY = (int)(fPlayerY * shapesize);

        Point[] square = new Point[4];
        int halfSize = shapesize;

        for (int i = 0; i < 4; i++)
        {
            double angle = fPlayerA + i * Math.PI / 2 + Math.PI / 4;
            square[i] = new Point(
                playerScreenX + (int)(Math.Cos(angle) * halfSize),
                playerScreenY + (int)(Math.Sin(angle) * halfSize)
            );
        }

        using (SolidBrush brushSquare = new SolidBrush(Color.FromArgb(196, 209, 79)))
        {
            g.FillPolygon(brushSquare, square);
        }

        Point[] triangle = new Point[3];
        int triangleOffset = shapesize;

        triangle[0] = new Point(
            playerScreenX + (int)(Math.Cos(fPlayerA) * (halfSize + triangleOffset)),
            playerScreenY + (int)(Math.Sin(fPlayerA) * (halfSize + triangleOffset))
        );
        triangle[1] = new Point(
            playerScreenX + (int)(Math.Cos(fPlayerA + Math.PI / 2) * halfSize),
            playerScreenY + (int)(Math.Sin(fPlayerA + Math.PI / 2) * halfSize)
        );
        triangle[2] = new Point(
            playerScreenX + (int)(Math.Cos(fPlayerA - Math.PI / 2) * halfSize),
            playerScreenY + (int)(Math.Sin(fPlayerA - Math.PI / 2) * halfSize)
        );

        using (SolidBrush brushTriangle = new SolidBrush(Color.FromArgb(196, 209, 79)))
        {
            g.FillPolygon(brushTriangle, triangle);
        }
    }

    static void Draw(IntPtr hWnd)
    {
        PAINTSTRUCT ps;
        IntPtr hdc = BeginPaint(hWnd, out ps);

        offscreenGraphics.Clear(Color.FromArgb(217, 217, 217));

        GetClientRect( hWnd, out RECT lpRect);
        nScreenWidth = lpRect.right - lpRect.left; 
        nScreenHeight = lpRect.bottom - lpRect.top; 

        DrawGraphics(offscreenGraphics);
        DrawMap(offscreenGraphics);
        Drawray(offscreenGraphics);
        DrawPlayer(offscreenGraphics);
        DrawRemotePlayer(offscreenGraphics);

        using (Graphics screenGraphics = Graphics.FromHdc(hdc))
        {
            screenGraphics.DrawImage(offscreenBitmap, 0, 0);
        }

        EndPaint(hWnd, ref ps);
    }

    static void DrawRemotePlayer(Graphics g)
    {
        int shapesize = mapsize / map.Length;
        int playerScreenX = (int)(multiplayerInstance.RemotePlayerX * shapesize);
        int playerScreenY = (int)(multiplayerInstance.RemotePlayerY * shapesize);

        using (SolidBrush brush = new SolidBrush(Color.Red))
        {
            g.FillEllipse(brush, playerScreenX, playerScreenY, 10, 10);
        }
    }
    static void MainWhile(IntPtr hWnd)
    {
        BtnPress(ConsoleKey.F1, () => ToggleConsole());
        
        BtnEnter(ConsoleKey.W, () => {
            fPlayerX += (PlayerSpeed * (float)Math.Cos(fPlayerA));
            fPlayerY += (PlayerSpeed * (float)Math.Sin(fPlayerA));
            });
        BtnEnter(ConsoleKey.S, () => {
            fPlayerX += -(PlayerSpeed * (float)Math.Cos(fPlayerA));
            fPlayerY += -(PlayerSpeed * (float)Math.Sin(fPlayerA));
        });
        BtnEnter(ConsoleKey.D, () => {
            fPlayerX += (PlayerSpeed * (float)Math.Cos(fPlayerA + 1.5708));
            fPlayerY += (PlayerSpeed * (float)Math.Sin(fPlayerA + 1.5708));
        });
        BtnEnter(ConsoleKey.A, () => {
            fPlayerX += (PlayerSpeed * -(float)Math.Cos(fPlayerA + 1.5708));
            fPlayerY += (PlayerSpeed * -(float)Math.Sin(fPlayerA + 1.5708));
        });
        BtnEnter(ConsoleKey.Q, () => fPlayerA += -PlayerSencity);
        BtnEnter(ConsoleKey.E, () => fPlayerA += PlayerSencity);

        multiplayerInstance.SendData(fPlayerX, fPlayerY, fPlayerA);


        Draw(hWnd);
        
    }

    static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        switch (msg)
        {
            case WM_CLOSE:
                DestroyWindow(hWnd);
                return IntPtr.Zero;
            case WM_DESTROY:
                PostQuitMessage(0);
                return IntPtr.Zero;
            case WM_PAINT:
                MainWhile(hWnd);
                return IntPtr.Zero;
            case WM_SIZE:
                int width = (short)(lParam.ToInt32() & 0xffff);
                int height = (short)((lParam.ToInt32() >> 16) & 0xffff);
                UpdateBitmapSize(width, height);
                InvalidateRect(hWnd, IntPtr.Zero, true); // Перемальовуємо вікно
                return IntPtr.Zero;
        }
        return DefWindowProc(hWnd, msg, wParam, lParam);
    }
    static System.Threading.Timer updateTimer;

    static void UpdateBitmapSize(int width, int height)
    {
        if (offscreenBitmap != null)
        {
            offscreenBitmap.Dispose(); // Звільняємо попередній Bitmap
        }

        offscreenBitmap = new Bitmap(width, height);
        offscreenGraphics = Graphics.FromImage(offscreenBitmap);
    }

    static void Main()
    {
        multiplayerInstance = new Multiplayer();
        multiplayerInstance.Connect("127.0.0.1", 12345);
        IntPtr hInstance = GetModuleHandle(null);

        WNDCLASS wndClass = new WNDCLASS
        {
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate((WndProcDelegate)WndProc),
            hInstance = hInstance,
            lpszClassName = "MyWindowClass"
        };

        RegisterClass(ref wndClass);

        IntPtr hWnd = CreateWindowEx(
            0, "MyWindowClass", "Вікно з Кубом", WS_OVERLAPPEDWINDOW, 100, 100, 800, 600,
            IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

        ShowWindow(hWnd, 1);

        // Запускаємо таймер для періодичного оновлення
        //updateTimer = new System.Threading.Timer(_ => InvalidateRect(hWnd, IntPtr.Zero, true), null, Timeout.Infinite, Timeout.Infinite);
        updateTimer = new System.Threading.Timer(_ => InvalidateRect(hWnd, IntPtr.Zero, true), null, 0, 1); // 33 мс для ~30 FPS

        MSG msg;
        while (GetMessage(out msg, IntPtr.Zero, 0, 0))
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }

        // Зупинка таймера перед закриттям програми
        updateTimer.Dispose();
    }

    static Dictionary<ConsoleKey, bool> keyStates = new Dictionary<ConsoleKey, bool>();

    static void BtnPress(ConsoleKey key, Action onPress)
    {
        if (!keyStates.ContainsKey(key))
            keyStates[key] = false;

        if (Keyboard.IsKeyPressed(key) && !keyStates[key])
        {
            onPress();
            keyStates[key] = true;
        }
        else if (!Keyboard.IsKeyPressed(key) && keyStates[key])
        {
            keyStates[key] = false;
        }
    }
    static void BtnEnter(ConsoleKey key, Action onHold)
    {
        if (Keyboard.IsKeyPressed(key))
        {
            onHold();
        }
    }
    static void ToggleConsole()
    {
        IntPtr consoleWindow = GetConsoleWindow();
        if (consoleWindow == IntPtr.Zero)
        {
            AllocConsole();
            consoleVisible = true;
        }
        else
        {
            ShowWindow(consoleWindow, consoleVisible ? 0 : 1);
            consoleVisible = !consoleVisible;
        }
    }

    public static class Keyboard
    {
        public static bool IsKeyPressed(ConsoleKey key)
        {
            return (GetAsyncKeyState((int)key) & 0x8000) != 0;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    static extern short RegisterClass(ref WNDCLASS lpWndClass);

    delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
}
