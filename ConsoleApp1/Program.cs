using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;

internal class Program
{
    private static void Main(string[] args)
    {
        var st = new Stopwatch();

        int task1Win = 0;
        int task2Win = 0;

        var srcWidth = 512 * 4;
        var srcHeight = 64 * 4;
        var dstWidth = srcWidth;
        var dstHeight = srcHeight;
        var srcPixels = new Color[srcWidth * srcHeight];
        var dstPixels = new Color[dstWidth * dstHeight];
        while (true)
        {
            Console.WriteLine("");
            Console.WriteLine("");


            st.Restart();
            Parallel.For(0, dstWidth, x =>
            {
                int sourceIndex = x * srcHeight;
                int targetIndex = x * dstHeight;
                Array.Copy(srcPixels, sourceIndex, dstPixels, targetIndex, srcHeight);
            });
            st.Stop();
            double task1 = st.Elapsed.TotalMilliseconds;
            Console.WriteLine("part 1: " + st.Elapsed.TotalMilliseconds);

            st.Restart();
            Parallel.For(0, dstHeight, y =>
            {
                int sourceIndex = y * srcWidth;
                int targetIndex = y * dstWidth;
                Array.Copy(srcPixels, sourceIndex, dstPixels, targetIndex, dstWidth);
            });
            st.Stop();
            double task2 = st.Elapsed.TotalMilliseconds;
            Console.WriteLine("part 2: " + st.Elapsed.TotalMilliseconds);

            if (task1 < task2)
            {
                task1Win++;
                Console.WriteLine("winter is Task1");
            }
            else
            {
                task2Win++;
                Console.WriteLine("winter is Task2");
            }

            var totalTask = task1Win + task2Win;
            Console.WriteLine("winRate Task1: " + (task1Win / (float)totalTask) * 100);

            Console.WriteLine("");
            Console.WriteLine("");
        }
    }
}