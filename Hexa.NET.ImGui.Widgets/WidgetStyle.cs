namespace Hexa.NET.ImGui.Widgets
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class WidgetStyle
    {
        public virtual string HomeIcon { get; set; } = $"{MaterialIcons.Home}";

        public virtual string BackIcon { get; set; } = $"{MaterialIcons.ArrowBack}";

        public virtual string ForwardIcon { get; set; } = $"{MaterialIcons.ArrowForward}";

        public virtual string RefreshIcon { get; set; } = $"{MaterialIcons.Refresh}";

        public virtual string CloseIcon { get; set; } = $"{MaterialIcons.Close}";

        public virtual string MinimizeIcon { get; set; } = $"{MaterialIcons.Minimize}";

        public virtual string FolderIcon { get; set; } = $"{MaterialIcons.Folder}";

        public virtual string FileIcon { get; set; } = $"{MaterialIcons.UnknownDocument}";

        public virtual string ComputerIcon { get; set; } = $"{MaterialIcons.Computer}";
    }
}