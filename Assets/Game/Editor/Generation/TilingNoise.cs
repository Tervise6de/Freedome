using UnityEngine;

namespace Freedome.EditorTools.Generation
{
    /// <summary>
    /// Small periodic value-noise library.
    ///
    /// Unity's Mathf.PerlinNoise does not tile, and every texture generated for this
    /// project has to tile seamlessly at one-metre intervals or the consistent
    /// texture scale across the shed would show visible seams. These functions take
    /// an explicit lattice period and wrap their integer coordinates, so the result
    /// is guaranteed periodic.
    /// </summary>
    public static class TilingNoise
    {
        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = (x * 374761393) + (y * 668265263) + (seed * 1442695040);
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0x7fffffff) / (float)0x7fffffff;
            }
        }

        private static int Wrap(int v, int period)
        {
            int m = v % period;
            return m < 0 ? m + period : m;
        }

        private static float Smooth(float t) => t * t * (3f - (2f * t));

        /// <summary>Periodic value noise. Inputs are in lattice units.</summary>
        public static float Value(float x, float y, int period, int seed)
        {
            int xi = Mathf.FloorToInt(x);
            int yi = Mathf.FloorToInt(y);
            float xf = Smooth(x - xi);
            float yf = Smooth(y - yi);

            float v00 = Hash(Wrap(xi, period), Wrap(yi, period), seed);
            float v10 = Hash(Wrap(xi + 1, period), Wrap(yi, period), seed);
            float v01 = Hash(Wrap(xi, period), Wrap(yi + 1, period), seed);
            float v11 = Hash(Wrap(xi + 1, period), Wrap(yi + 1, period), seed);

            return Mathf.Lerp(Mathf.Lerp(v00, v10, xf), Mathf.Lerp(v01, v11, xf), yf);
        }

        /// <summary>Periodic fractal noise. Each octave doubles the lattice period.</summary>
        public static float Fbm(float x, float y, int period, int seed, int octaves = 4, float gain = 0.5f)
        {
            float sum = 0f;
            float amplitude = 1f;
            float total = 0f;
            int p = period;
            float freq = 1f;

            for (int o = 0; o < octaves; o++)
            {
                sum += Value(x * freq, y * freq, p, seed + (o * 71)) * amplitude;
                total += amplitude;
                amplitude *= gain;
                freq *= 2f;
                p *= 2;
            }

            return total > 0f ? sum / total : 0f;
        }

        /// <summary>Ridged variant, useful for scratches and grain lines.</summary>
        public static float Ridge(float x, float y, int period, int seed, int octaves = 3)
        {
            float n = Fbm(x, y, period, seed, octaves);
            return 1f - Mathf.Abs((n * 2f) - 1f);
        }

        /// <summary>
        /// Periodic Worley/cellular noise returning the distance to the nearest feature
        /// point. Used for the spangle on galvanised steel and for gravel.
        /// </summary>
        public static float Cell(float x, float y, int period, int seed)
        {
            int xi = Mathf.FloorToInt(x);
            int yi = Mathf.FloorToInt(y);
            float best = 10f;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int cx = xi + dx;
                    int cy = yi + dy;
                    float px = cx + Hash(Wrap(cx, period), Wrap(cy, period), seed);
                    float py = cy + Hash(Wrap(cx, period), Wrap(cy, period), seed + 977);
                    float d = ((px - x) * (px - x)) + ((py - y) * (py - y));
                    if (d < best)
                    {
                        best = d;
                    }
                }
            }

            return Mathf.Sqrt(best);
        }
    }
}
