using System;
using System.IO;

namespace Bench.Util;

public class RaplMeter
{
    private readonly string _energyPath;
    private readonly string _maxPath;
    private readonly double _maxJoules;

    public RaplMeter(string raplDir = "/sys/class/powercap/intel-rapl:0")
    {
        _energyPath = Path.Combine(raplDir, "energy_uj");
        _maxPath    = Path.Combine(raplDir, "max_energy_range_uj");

        if (!File.Exists(_energyPath))
            throw new InvalidOperationException($"RAPL energy file not found at {_energyPath}");

        if (!File.Exists(_maxPath))
            throw new InvalidOperationException($"RAPL max energy file not found at {_maxPath}");

        _maxJoules = ReadMicroJoules(_maxPath) / 1_000_000.0;
    }

    private static double ReadMicroJoules(string path)
    {
        string txt = File.ReadAllText(path).Trim();
        return double.Parse(txt);
    }

    public double ReadJoules()
    {
        return ReadMicroJoules(_energyPath) / 1_000_000.0;
    }

    public double Delta(double start, double end)
    {
        // handle wraparound
        if (end < start) end += _maxJoules;
        return end - start;
    }
}
