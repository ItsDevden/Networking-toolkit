using System.Drawing;
using System.Windows.Forms;

namespace NetworkingToolkit
{
    public class DarkTabControl : TabControl
    {
        public DarkTabControl()
        {
            ResizeRedraw = true;
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg != 0x000F) return;

            using Graphics g = Graphics.FromHwnd(Handle);
            using var background = new SolidBrush(
                Color.FromArgb(33, 33, 33));
            using var selected = new SolidBrush(
                Color.FromArgb(48, 48, 48));

            Rectangle page = DisplayRectangle;

            // Cover the complete native tab header.
            g.FillRectangle(background,
                0, 0, ClientSize.Width, page.Top);

            for (int i = 0; i < TabCount; i++)
            {
                Rectangle tab = GetTabRect(i);

                if (i == SelectedIndex)
                    g.FillRectangle(selected, tab);

                TextRenderer.DrawText(
                    g,
                    TabPages[i].Text,
                    Font,
                    tab,
                    Color.White,
                    TextFormatFlags.HorizontalCenter |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.SingleLine);
            }

            // Cover the native page border.
            using var border = new Pen(
                Color.FromArgb(33, 33, 33), 4);

            Rectangle frame = page;
            frame.Inflate(2, 2);
            g.DrawRectangle(border, frame);
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            Invalidate();
        }
    }
}