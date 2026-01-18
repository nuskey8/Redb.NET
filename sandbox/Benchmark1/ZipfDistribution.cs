sealed class ZipfDistribution
{
    readonly Random random = new(42);
    readonly double[] cumulativeProbabilities;
    readonly int n;

    public ZipfDistribution(int n, double s = 1.0)
    {
        this.n = n;
        cumulativeProbabilities = new double[n];

        var sum = 0.0;
        for (int i = 1; i <= n; i++)
        {
            sum += 1.0 / Math.Pow(i, s);
        }

        var cumulative = 0.0;
        for (int i = 0; i < n; i++)
        {
            cumulative += (1.0 / Math.Pow(i + 1, s)) / sum;
            cumulativeProbabilities[i] = cumulative;
        }
    }

    public int Next()
    {
        var r = random.NextDouble();

        int left = 0;
        int right = n - 1;

        while (left < right)
        {
            int mid = (left + right) / 2;
            if (cumulativeProbabilities[mid] < r)
            {
                left = mid + 1;
            }
            else
            {
                right = mid;
            }
        }

        return left;
    }
}
