// ---------------------------------------------------------------------------
//  Copyright (c) 2026, The .NET Foundation.
//  This software is released under the Apache Licence, Version 2.0.
//  The licence and further copyright text can be found in the file LICENCE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Chem4Word.Model2.Cleaners.V2
{
    internal static class EmbeddingHelper
    {
        // Classical multidimensional scaling: coordinates whose distances best match 'd' in the least-squares sense.
        internal static double[][] ClassicalMds(double[,] d, int dims)
        {
            int m = d.GetLength(0);
            double[,] b = new double[m, m];
            double[] rowMean = new double[m];
            double grand = 0;
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    rowMean[i] += d[i, j] * d[i, j];
                }
                grand += rowMean[i];
                rowMean[i] /= m;
            }
            grand /= (double)m * m;

            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    b[i, j] = -0.5 * (d[i, j] * d[i, j] - rowMean[i] - rowMean[j] + grand);
                }
            }

            List<double[]> vectors = new List<double[]>();
            double[][] result = Enumerable.Range(0, m).Select(_ => new double[dims]).ToArray();
            for (int k = 0; k < dims; k++)
            {
                double[] v = TopEigenvector(b, out double lambda, vectors);
                vectors.Add(v);
                double scale = Math.Sqrt(Math.Max(lambda, 1e-6));
                for (int i = 0; i < m; i++)
                {
                    // Tiny deterministic jitter so nothing starts exactly on top of anything else.
                    result[i][k] = v[i] * scale + 1e-3 * Math.Sin(i * (2.3 + k) + 0.4 * k);
                }
            }
            return result;
        }

        // Power iteration on a shifted matrix (so the most positive eigenvalue dominates), orthogonal to 'exclude'.
        internal static double[] TopEigenvector(double[,] b, out double eigenvalue, List<double[]> exclude)
        {
            int m = b.GetLength(0);
            double shift = 0;
            for (int i = 0; i < m; i++)
            {
                double row = 0;
                for (int j = 0; j < m; j++)
                {
                    row += Math.Abs(b[i, j]);
                }
                shift = Math.Max(shift, row);
            }

            double[] v = Enumerable.Range(0, m).Select(i => Math.Sin(i * 1.31 + 0.7) + 0.01 * i).ToArray();
            double[] w = new double[m];
            for (int iter = 0; iter < 1000; iter++)
            {
                foreach (double[] u in exclude)
                {
                    Orthogonalize(v, u);
                }
                Normalize(v);
                for (int i = 0; i < m; i++)
                {
                    double s = shift * v[i];
                    for (int j = 0; j < m; j++)
                    {
                        s += b[i, j] * v[j];
                    }
                    w[i] = s;
                }
                (v, w) = (w, v);
            }
            foreach (double[] u in exclude)
            {
                Orthogonalize(v, u);
            }
            Normalize(v);

            eigenvalue = 0;
            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < m; j++)
                {
                    eigenvalue += v[i] * b[i, j] * v[j];
                }
            }
            return v;
        }

        private static void Orthogonalize(double[] v, double[] u)
        {
            double dot = Dot(v, u);
            for (int i = 0; i < v.Length; i++)
            {
                v[i] -= dot * u[i];
            }
        }

        private static void Normalize(double[] v)
        {
            double norm = Math.Sqrt(Dot(v, v));
            if (norm < 1e-12)
            {
                return;
            }
            for (int i = 0; i < v.Length; i++)
            {
                v[i] /= norm;
            }
        }

        internal static double Dot(double[] a, double[] b)
        {
            double sum = 0;
            for (int i = 0; i < a.Length; i++)
            {
                sum += a[i] * b[i];
            }
            return sum;
        }

        internal static double[] Subtract(double[] a, double[] b) => a.Select((t, i) => t - b[i]).ToArray();

        // Rodrigues' rotation of 3D vector v about unit 'axis'.
        internal static double[] RotateAbout(double[] v, double[] axis, double angle)
        {
            double c = Math.Cos(angle), s = Math.Sin(angle), dot = Dot(v, axis);
            double[] cross =
            {
                axis[1] * v[2] - axis[2] * v[1],
                axis[2] * v[0] - axis[0] * v[2],
                axis[0] * v[1] - axis[1] * v[0]
            };
            return Enumerable.Range(0, 3).Select(i => v[i] * c + cross[i] * s + axis[i] * dot * (1 - c)).ToArray();
        }

        // Scale so the median bond length is exactly one.
        internal static void ScaleToUnitBonds(double[][] p, List<DistanceTerm> terms)
        {
            List<double> lengths = terms.Where(t => t.IsBond)
                                        .Select(t => Math.Sqrt(p[t.I].Select((x, d) => (x - p[t.J][d]) * (x - p[t.J][d])).Sum()))
                                        .OrderBy(l => l)
                                        .ToList();
            if (lengths.Count == 0 || lengths[lengths.Count / 2] < 1e-9)
            {
                return;
            }

            double k = 1 / lengths[lengths.Count / 2];
            foreach (double[] q in p)
            {
                for (int d = 0; d < q.Length; d++)
                {
                    q[d] *= k;
                }
            }
        }

        // Minimises Σ C(|xi - xj|² - dij²)², lower-bound terms only counting while too close.
        internal static void MinimizeDistances(double[][] p, List<DistanceTerm> terms, int maxIterations = 3000)
        {
            int m = p.Length, dims = p[0].Length;
            double[] v = p.SelectMany(q => q).ToArray();

            Lbfgs(v, (x, g) =>
            {
                Array.Clear(g, 0, g.Length);
                double f = 0;
                foreach (DistanceTerm t in terms)
                {
                    double r2 = 0;
                    for (int d = 0; d < dims; d++)
                    {
                        double delta = x[t.I * dims + d] - x[t.J * dims + d];
                        r2 += delta * delta;
                    }

                    double diff = r2 - t.Target2;
                    if (t.LowerBoundOnly && diff >= 0)
                    {
                        continue;
                    }

                    f += t.Weight * diff * diff;
                    double scale = 4 * t.Weight * diff;
                    for (int d = 0; d < dims; d++)
                    {
                        double delta = x[t.I * dims + d] - x[t.J * dims + d];
                        g[t.I * dims + d] += scale * delta;
                        g[t.J * dims + d] -= scale * delta;
                    }
                }
                return f;
            }, maxIterations);

            for (int i = 0; i < m; i++)
            {
                for (int d = 0; d < dims; d++)
                {
                    p[i][d] = v[i * dims + d];
                }
            }
        }

        // Limited-memory BFGS with a backtracking (Armijo) line search; a quasi-Newton stand-in for the paper's truncated Newton.
        private static void Lbfgs(double[] x, Func<double[], double[], double> evaluate, int maxIterations)
        {
            const int history = 7;
            int n = x.Length;
            double[] g = new double[n], trialX = new double[n], trialG = new double[n];
            double f = evaluate(x, g);
            List<double[]> s = new List<double[]>(), y = new List<double[]>();
            List<double> rho = new List<double>();

            for (int iter = 0; iter < maxIterations; iter++)
            {
                if (Math.Sqrt(Dot(g, g)) < 1e-10)
                {
                    break;
                }

                double[] q = (double[])g.Clone();
                double[] alpha = new double[s.Count];
                for (int i = s.Count - 1; i >= 0; i--)
                {
                    alpha[i] = rho[i] * Dot(s[i], q);
                    for (int k = 0; k < n; k++)
                    {
                        q[k] -= alpha[i] * y[i][k];
                    }
                }

                double gamma = s.Count > 0 ? Dot(s[s.Count - 1], y[s.Count - 1]) / Dot(y[s.Count - 1], y[s.Count - 1]) : 1 / Math.Max(1, Math.Sqrt(Dot(g, g)));
                for (int k = 0; k < n; k++)
                {
                    q[k] *= gamma;
                }
                for (int i = 0; i < s.Count; i++)
                {
                    double beta = rho[i] * Dot(y[i], q);
                    for (int k = 0; k < n; k++)
                    {
                        q[k] += s[i][k] * (alpha[i] - beta);
                    }
                }

                double[] direction = q.Select(t => -t).ToArray();
                double slope = Dot(direction, g);
                if (slope >= 0)
                {
                    direction = g.Select(t => -t).ToArray();
                    slope = -Dot(g, g);
                    s.Clear();
                    y.Clear();
                    rho.Clear();
                }

                double step = 1, trialF;
                while (true)
                {
                    for (int k = 0; k < n; k++)
                    {
                        trialX[k] = x[k] + step * direction[k];
                    }
                    trialF = evaluate(trialX, trialG);
                    if (trialF <= f + 1e-4 * step * slope || step < 1e-12)
                    {
                        break;
                    }
                    step *= 0.5;
                }
                if (step < 1e-12)
                {
                    break;
                }

                double[] sk = trialX.Select((t, k) => t - x[k]).ToArray();
                double[] yk = trialG.Select((t, k) => t - g[k]).ToArray();
                double sy = Dot(sk, yk);
                if (sy > 1e-12)
                {
                    s.Add(sk);
                    y.Add(yk);
                    rho.Add(1 / sy);
                    if (s.Count > history)
                    {
                        s.RemoveAt(0);
                        y.RemoveAt(0);
                        rho.RemoveAt(0);
                    }
                }

                double previous = f;
                Array.Copy(trialX, x, n);
                Array.Copy(trialG, g, n);
                f = trialF;
                if (previous - f < 1e-12 * Math.Max(1, Math.Abs(f)))
                {
                    break;
                }
            }
        }
    }
}
