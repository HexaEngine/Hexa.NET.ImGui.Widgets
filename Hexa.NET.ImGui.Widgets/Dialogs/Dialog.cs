using System.Xml.Linq;

namespace Hexa.NET.ImGui.Widgets.Dialogs
{
    using Hexa.NET.ImGui;
    using System.Numerics;
    using System.Runtime.CompilerServices;

    public delegate void DialogCallback(object? sender, DialogResult result);

    [Flags]
    public enum DialogFlags
    {
        None = 1 << 0,
        CenterOnParent = 1 << 1,
        AlwaysCenter = 1 << 2
    }

    public abstract class Dialog : IDialog
    {
        private bool windowEnded;
        private bool shown;
        protected DialogCallback? callback;
        private Vector2 windowPos;
        private Vector2 windowSize;
        private IDialog? parent;
        private DialogFlags flags;
        private bool firstFrame = true;

        public Vector2 InitialSize { get; set; } = new(1000, 600);

        public DialogResult Result { get; protected set; }

        public abstract string Name { get; }

        protected abstract ImGuiWindowFlags Flags { get; }

        public Vector2 WindowPos => windowPos;

        public Vector2 WindowSize => windowSize;

        public bool Shown => shown;

        public object? Userdata { get; set; }

        public unsafe void Draw(ImGuiWindowFlags overwriteFlags)
        {
            if (!shown) return;

            var windowFlags = Flags | overwriteFlags;

            bool wasOpen = shown;
            if (!ImGui.Begin(Name, ref shown, windowFlags))
            {
                if (wasOpen && wasOpen != shown)
                {
                    Close();
                }
                ImGui.End();
                return;
            }

            if (wasOpen && wasOpen != shown)
            {
                Close();
            }

            windowPos = ImGui.GetWindowPos();
            windowSize = ImGui.GetWindowSize();

            DrawContent();

            windowEnded = false;

            if (firstFrame)
            {
                ImGuiP.SetWindowSize(InitialSize);
                firstFrame = false;
            }
            else
            {
                if ((flags & DialogFlags.CenterOnParent) != 0 && parent != null)
                {
                    ImGuiP.SetWindowPos(parent.WindowPos + (parent.WindowSize - windowSize) / 2);
                    if ((flags & DialogFlags.AlwaysCenter) == 0)
                    {
                        flags &= ~DialogFlags.CenterOnParent;
                    }
                }
            }

            if (!windowEnded)
                ImGui.End();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void EndDraw()
        {
            ImGui.End();
            windowEnded = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract void DrawContent();

        public virtual void Close()
        {
            shown = false;
            DialogManager.CloseDialog(this);
            callback?.Invoke(this, Result);
            callback = null; // clear callback afterwards to prevent memory leaks
        }

        protected virtual void Close(DialogResult result)
        {
            Result = result;
            Close();
        }

        public virtual void Reset()
        {
            Result = DialogResult.None;
        }

        public virtual void Show()
        {
            DialogManager.ShowDialog(this);
            shown = true;
        }

        public virtual void Show(DialogCallback callback)
        {
            this.callback = callback;
            Show();
        }

        public virtual void Show(DialogCallback callback, IDialog parent, DialogFlags flags)
        {
            this.flags = flags;
            this.parent = parent;
            this.callback = callback;
            Show();
        }
    }
}