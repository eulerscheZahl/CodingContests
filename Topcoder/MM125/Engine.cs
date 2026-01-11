using System.Drawing;

public abstract class Engine
{
    public int N;
    public int C;
    public abstract int Play(Strategy strategy);
    public abstract void PrintStats(Strategy strategy);

    private static Brush[] colors = new Brush[] { Brushes.Red, Brushes.Cyan, Brushes.Green, Brushes.Blue, Brushes.Orange, Brushes.Magenta, Brushes.Black, Brushes.Pink, Brushes.Gray };
    public void Plot(int[] grid, Point p1, Point p2, int turn)
    {
#if DEBUG
        int size = 50;
        grid[p2.X * N + p2.Y] = grid[p1.X * N + p1.Y];
        grid[p1.X * N + p1.Y] = 0;
        Bitmap bmp = new Bitmap(size * N, size * N);
        Graphics g = Graphics.FromImage(bmp);
        g.Clear(Color.White);
        for (int x = 0; x < N; x++)
        {
            for (int y = 0; y < N; y++)
            {
                if (grid[x * N + y] == 0) continue;
                g.FillEllipse(colors[grid[x * N + y] - 1], x * size, y * size, size, size);
            }
        }
        g.DrawRectangle(Pens.Red, p1.X * size, p1.Y * size, size, size);
        g.DrawRectangle(Pens.Red, p2.X * size, p2.Y * size, size, size);
        g.Dispose();
        bmp.Save("plot/" + turn.ToString("000") + ".png");
        bmp.Dispose();
#endif
    }
}