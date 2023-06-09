using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

using static System.Console;

const int N = 100;
Tester tester = new Tester(N);

tester.TestSillyCopy();
tester.TestArrayCopy();
tester.TestBlockCopy();
tester.TestPointerCopy();
tester.TestUnroalingCopy();
tester.TestSseCopy();
tester.TestParalleCopy();

public class Tester
{
    private Stopwatch sw;

    private const int smallSize = 10_000;
    private const int mediumSize = 1_000_000;
    private const int bigSize = 100_000_000;

    private float[] smallSource = new float[smallSize];
    private float[] mediumSource = new float[mediumSize];
    private float[] bigSource = new float[bigSize];

    private float[] smallTarget = new float[smallSize];
    private float[] mediumTarget = new float[mediumSize];
    private float[] bigTarget = new float[bigSize];

    private int N = 0;

    public Tester(int N)
    {
        this.sw = new Stopwatch();
        this.N = N;
        
        var rand = Random.Shared;
        for (int i = 0; i < smallSize; i++)
            smallSource[i] = rand.NextSingle();

        for (int i = 0; i < mediumSize; i++)
            mediumSource[i] = rand.NextSingle();

        for (int i = 0; i < bigSize; i++)
            bigSource[i] = rand.NextSingle();
    }

    public void TestSillyCopy()
    {
        Test(
            CopyAlgorithms.SillyCopy, "Silly Copy"
        );
    }
    
    public void TestArrayCopy()
    {
        Test(
            CopyAlgorithms.ArrayCopy, "Array Copy"
        );
    }

    public void TestBlockCopy()
    {
        
        Test(
            CopyAlgorithms.BlockCopy, "Block Copy"
        );
    }

    public void TestPointerCopy()
    {
        Test(
            CopyAlgorithms.PointerCopy, "Pointer Copy"
        );
    }

    public void TestUnroalingCopy()
    {
        Test(
            CopyAlgorithms.UnroalingCopy, "Unroaling Copy"
        );
    }
    
    public void TestSseCopy()
    {
        Test(
            CopyAlgorithms.SseCopy, "Sse Copy"
        );
    }

    public void TestParalleCopy()
    {
        Test(
            CopyAlgorithms.ParallelCopy, "Parallel Copy"
        );
    }

    public void Test(Action<float[], float[]> copy, string name)
    {
        sw.Start();
        for (int i = 0; i < N; i++)
            copy(smallSource, smallTarget);
        sw.Stop();
        var small = sw.Elapsed;
        sw.Reset();
        
        sw.Start();
        for (int i = 0; i < N; i++)
            copy(mediumSource, mediumTarget);
        sw.Stop();
        var medium = sw.Elapsed;
        sw.Reset();
        
        sw.Start();
        for (int i = 0; i < N; i++)
            copy(bigSource, bigTarget);
        sw.Stop();
        var big = sw.Elapsed;
        sw.Reset();

        WriteLine(
            $"""
                {name}: 
                    small  : {small.TotalMicroseconds / N} us
                    medium : {medium.TotalMicroseconds / N} us
                    big    : {big.TotalMicroseconds / N} us
            """
        );
    }
}

public static class CopyAlgorithms
{
    public static void SillyCopy(float[] source, float[] target)
    {
        for (int i = 0; i < source.Length; i++)
            target[i] = source[i];
    }

    public static void ArrayCopy(float[] source, float[] target)
    {
        Array.Copy(source, target, source.Length);
    }

    public static void BlockCopy(float[] source, float[] target)
    {
        Buffer.BlockCopy(source, 0, target, 0, sizeof(float) * source.Length);
    }

    public static unsafe void PointerCopy(float[] source, float[] target)
    {
        fixed (float* sourcePointer = source, targetPointer = target)
        {
            var end = sourcePointer + source.Length;
            var s = sourcePointer;
            var t = targetPointer;

            for (; s < end; s++, t++)
                *t = *s;
        }
    }

    public static unsafe void UnroalingCopy(float[] source, float[] target)
    {
        fixed (float* sourcePointer = source, targetPointer = target)
        {
            const int jump = 8;
            var end = sourcePointer + source.Length;
            var s = sourcePointer;
            var t = targetPointer;

            do
            {
                *(t + 0) = *(s + 0);
                *(t + 1) = *(s + 1);
                *(t + 2) = *(s + 2);
                *(t + 3) = *(s + 3);
                *(t + 4) = *(s + 4);
                *(t + 5) = *(s + 5);
                *(t + 6) = *(s + 6);
                *(t + 7) = *(s + 7);
                s += jump;
                t += jump;
            } while (s < end);
        }
    }
    
    public static unsafe void SseCopy(float[] source, float[] target)
    {
        fixed (float* sourcePointer = source, targetPointer = target)
        {
            const int jump = 16;
            var end = sourcePointer + source.Length;
            var s = sourcePointer;
            var t = targetPointer;

            do
            {
                var vs = Sse42.LoadVector128(s);
                Sse42.Store(t, vs);
                
                vs = Sse42.LoadVector128(s + 4);
                Sse42.Store(t + 4, vs);
                
                vs = Sse42.LoadVector128(s + 8);
                Sse42.Store(t + 8, vs);
                
                vs = Sse42.LoadVector128(s + 12);
                Sse42.Store(t + 12, vs);

                s += jump;
                t += jump;
            } while (s < end);
        }
    }

    public static unsafe void ParallelCopy(float[] source, float[] target)
    {
        fixed (float* sourcePointer = source)
        {
            var tempS = sourcePointer;
            int step = 100_000;

            Parallel.For(0, source.Length / step, i =>
            {
                var s = tempS + i * step;
                Marshal.Copy((nint)s, target, 0, step);
            });
        }
    }
}