namespace TestApp
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets;
    using Hexa.NET.ImGui.Widgets.Dialogs;
    using Silk.NET.Core.Contexts;
    using Silk.NET.OpenAL;
    using Silk.NET.OpenGL;
    using Silk.NET.SDL;
    using TestApp.ImGuiDemo;
    using TestApp.Input;

    public unsafe class Program : IGLContextSource
    {
        internal static readonly Sdl sdl = Sdl.GetApi();
        private static bool exiting = false;
        private static readonly List<Func<Event, bool>> hooks = new();
        private static Window* mainWindow;
        private static uint mainWindowId;

        private static int width;
        private static int height;

        private static ImGuiManager imGuiManager;
        private static void* glcontext;
        private static SDLGLContext context;
        private static GL gl;

        public static int Width => width;

        public static int Height => height;

        public IGLContext? GLContext { get; }

        public static event EventHandler<ResizedEventArgs>? Resized;

        private static void Main(string[] args)
        {
            sdl.SetHint(Sdl.HintMouseFocusClickthrough, "1");
            sdl.SetHint(Sdl.HintMouseAutoCapture, "0");
            sdl.SetHint(Sdl.HintAutoUpdateJoysticks, "1");
            sdl.SetHint(Sdl.HintJoystickHidapiPS4, "1");
            sdl.SetHint(Sdl.HintJoystickHidapiPS4Rumble, "1");
            sdl.SetHint(Sdl.HintJoystickRawinput, "0");
            sdl.Init(Sdl.InitEvents + Sdl.InitVideo + Sdl.InitGamecontroller + Sdl.InitHaptic + Sdl.InitJoystick + Sdl.InitSensor);

            sdl.GLSetAttribute(GLattr.ContextMajorVersion, 3);
            sdl.GLSetAttribute(GLattr.ContextMinorVersion, 3);
            sdl.GLSetAttribute(GLattr.ContextProfileMask, (int)GLprofile.Core);

            Keyboard.Init();
            Mouse.Init();

            int width = 1280;
            int height = 720;
            int y = 100;
            int x = 100;

            WindowFlags flags = WindowFlags.Resizable | WindowFlags.Hidden | WindowFlags.AllowHighdpi | WindowFlags.Opengl;
            mainWindow = sdl.CreateWindow("", x, y, width, height, (uint)flags);
            mainWindowId = sdl.GetWindowID(mainWindow);

            InitGraphics(mainWindow);
            InitImGui(mainWindow);

            sdl.ShowWindow(mainWindow);

            Time.Initialize();

            Event evnt;
            while (!exiting)
            {
                sdl.PumpEvents();
                while (sdl.PollEvent(&evnt) == (int)SdlBool.True)
                {
                    for (int i = 0; i < hooks.Count; i++)
                    {
                        hooks[i](evnt);
                    }

                    HandleEvent(evnt);
                }

                Render();

                Keyboard.Flush();
                Mouse.Flush();
                Time.FrameUpdate();
            }

            WidgetManager.Dispose();
            imGuiManager.Dispose();

            context.Dispose();

            sdl.DestroyWindow(mainWindow);

            //sdl.Quit();
        }

        private static void Render()
        {
            imGuiManager.NewFrame();

            WidgetManager.Draw();

            context.MakeCurrent();
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            gl.Clear((uint)ClearBufferMask.ColorBufferBit | (uint)ClearBufferMask.DepthBufferBit);

            imGuiManager.EndFrame();

            context.MakeCurrent();
            context.SwapBuffers();
        }

        private static void InitGraphics(Window* mainWindow)
        {
            glcontext = sdl.GLCreateContext(mainWindow);
            context = new(mainWindow, glcontext, null);
            gl = GL.GetApi(context);
        }

        private static void InitImGui(Window* mainWindow)
        {
            imGuiManager = new(mainWindow, gl, context);

            WidgetManager.Register<WidgetDemo>(show: true);

            WidgetManager.Init();
        }

        private static void Resize(int width, int height, int oldWidth, int oldHeight)
        {
            gl.Viewport(0, 0, (uint)width, (uint)height);
            Resized?.Invoke(null, new(width, height, oldWidth, oldHeight));
        }

        public static void RegisterHook(Func<Event, bool> hook)
        {
            hooks.Add(hook);
        }

        private static void HandleEvent(Event evnt)
        {
            EventType type = (EventType)evnt.Type;
            switch (type)
            {
                case EventType.Windowevent:
                    {
                        var even = evnt.Window;
                        if (even.WindowID == mainWindowId)
                        {
                            switch ((WindowEventID)evnt.Window.Event)
                            {
                                case WindowEventID.Close:
                                    exiting = true;
                                    break;

                                case WindowEventID.Resized:
                                    int oldWidth = Program.width;
                                    int oldHeight = Program.height;
                                    int width = even.Data1;
                                    int height = even.Data2;
                                    Resize(width, height, oldWidth, oldHeight);
                                    Program.width = width;
                                    Program.height = height;
                                    break;
                            }
                        }
                    }
                    break;

                case EventType.Mousemotion:
                    {
                        var even = evnt.Motion;
                        Mouse.OnMotion(even);
                    }
                    break;

                case EventType.Mousebuttondown:
                    {
                        var even = evnt.Button;
                        Mouse.OnButtonDown(even);
                    }
                    break;

                case EventType.Mousebuttonup:
                    {
                        var even = evnt.Button;
                        Mouse.OnButtonUp(even);
                    }
                    break;

                case EventType.Mousewheel:
                    {
                        var even = evnt.Wheel;
                        Mouse.OnWheel(even);
                    }
                    break;

                case EventType.Keydown:
                    {
                        var even = evnt.Key;
                        Keyboard.OnKeyDown(even);
                    }
                    break;

                case EventType.Keyup:
                    {
                        var even = evnt.Key;
                        Keyboard.OnKeyUp(even);
                    }
                    break;

                case EventType.Textediting:
                    break;

                case EventType.Textinput:
                    {
                        var even = evnt.Text;
                        Keyboard.OnTextInput(even);
                    }
                    break;

                case EventType.Dropfile:
                    sdl.Free(evnt.Drop.File);
                    break;
            }
        }

        // Move to Hexa.NET.Utilities later
    }
}