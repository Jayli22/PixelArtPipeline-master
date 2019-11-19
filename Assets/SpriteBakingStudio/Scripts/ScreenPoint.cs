namespace SBS
{
    public class ScreenPoint
    {
        public int x, y;

        public ScreenPoint()
        {
            x = 0;
            y = 0;
        }

        public ScreenPoint(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public override string ToString()
        {
            return x + ", " + y;
        }

        static public ScreenPoint operator+(ScreenPoint p1, ScreenPoint p2)
        {
            return new ScreenPoint(p1.x + p2.x, p1.y + p2.y);
        }

        static public ScreenPoint operator-(ScreenPoint p1, ScreenPoint p2)
        {
            return new ScreenPoint(p1.x - p2.x, p1.y - p2.y);
        }
    }
}
