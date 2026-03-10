using System.Drawing;
using System.Windows.Forms;

namespace DeskChange
{
    internal static class AppIconProvider
    {
        public static Icon Load()
        {
            Icon icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            if (icon != null)
            {
                return (Icon)icon.Clone();
            }

            return (Icon)SystemIcons.Application.Clone();
        }
    }
}
