using System.Drawing;

namespace FileSizer
{
    class Button
    {
        private Rectangle postion;
        private string text;
        private Color color;
        private bool activated;
        private Color? deactivatedColor;
        private Color? borderColor;

        public Button(Rectangle postion, string text, Color color, bool activated = false, 
            Color? deactivatedColor = null, Color? borderColor = null)
        {
            this.postion = postion;
            this.text = text;
            this.color = color;
            this.activated = activated;
            this.deactivatedColor = deactivatedColor;
            this.borderColor = borderColor;
        }

        public bool IsPressed(int x, int y)
        {
            return x > postion.X && x < postion.X + postion.Width && y > postion.Y 
                && y < postion.Y + postion.Height;
        }

        public void Draw(Graphics g)
        {
            
            if (!activated && deactivatedColor != null)
            {
                g.FillRectangle(new SolidBrush(deactivatedColor ?? default(Color)), postion);
            }
            else
            {
                g.FillRectangle(new SolidBrush(color), postion);
            }
            if (borderColor != null)
            {
                g.DrawRectangle(new Pen(new SolidBrush(borderColor ?? default(Color))), postion);
            }
            g.DrawString(text, new Font(FontFamily.GenericSerif, 12), new SolidBrush(Color.Black), postion);
        }

        public bool IsActivated()
        {
            return activated;
        }

        public void SetActivated(bool value)
        {
            activated = value;
        }
    }
}
