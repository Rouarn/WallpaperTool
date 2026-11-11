using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WallpaperTool.Views
{
    /// <summary>
    /// 记录「显式隐藏」的控件。
    /// Control.Visible 是有效可见性：父控件暂时不可见时子控件也会返回 false，
    /// 布局若据此跳过子控件，会在一瞬间把高度算成 0，因此布局只信任这里记录的显式状态。
    /// </summary>
    internal static class Visibility
    {
        private static readonly Dictionary<Control, bool> Hidden = new Dictionary<Control, bool>();

        public static void Set(Control control, bool visible)
        {
            if (control == null)
            {
                return;
            }

            Hidden[control] = !visible;
            control.Visible = visible;
        }

        public static bool IsHidden(Control control)
        {
            bool hidden;
            return Hidden.TryGetValue(control, out hidden) && hidden;
        }
    }
}
