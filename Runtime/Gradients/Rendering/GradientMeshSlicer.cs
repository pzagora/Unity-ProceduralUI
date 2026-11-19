using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ProceduralUI
{
    internal sealed class GradientMeshSlicer
    {
        private readonly List<UIVertex> _input = new();
        private readonly List<UIVertex> _output = new();
        private readonly List<float> _stops = new();
        private readonly GradientVertex[] _triangle = new GradientVertex[3];
        private readonly GradientVertex[] _first = new GradientVertex[6];
        private readonly GradientVertex[] _second = new GradientVertex[6];
        private Gradient _cachedRamp;
        private int _cachedRevision;

        private const int MAX_VERTICES = 64998;
        private const int PERCEPTUAL_SAMPLES = 32;

        internal bool TryApply(VertexHelper vertices, in GradientSettings gradient, GradientSpace space)
        {
            if (vertices.currentIndexCount > MAX_VERTICES)
                return false;

            UpdateStops(gradient);
            if (_stops.Count == 2 && gradient.ramp.mode == GradientMode.Blend)
                return false;

            _input.Clear();
            _output.Clear();
            vertices.GetUIVertexStream(_input);

            for (var index = 0; index < _input.Count; index += 3)
            {
                for (var corner = 0; corner < 3; corner++)
                {
                    var vertex = _input[index + corner];
                    _triangle[corner] = new GradientVertex(vertex, space.Project(vertex.position).y);
                }

                for (var band = 0; band < _stops.Count - 1; band++)
                {
                    var lower = band == 0 ? float.NegativeInfinity : _stops[band];
                    var upper = band == _stops.Count - 2 ? float.PositiveInfinity : _stops[band + 1];
                    if ((_triangle[0].Position < lower && _triangle[1].Position < lower && _triangle[2].Position < lower) ||
                        (_triangle[0].Position > upper && _triangle[1].Position > upper && _triangle[2].Position > upper))
                        continue;

                    var count = Clip(_triangle, 3, _first, lower, true);
                    count = Clip(_first, count, _second, upper, false);
                    if (count < 3)
                        continue;
                    if (_output.Count + (count - 2) * 3 > MAX_VERTICES)
                        return false;

                    var sample = (_stops[band] + _stops[band + 1]) * 0.5f;
                    var first = Colorize(_second[0], gradient, sample);
                    for (var corner = 1; corner < count - 1; corner++)
                    {
                        _output.Add(first);
                        _output.Add(Colorize(_second[corner], gradient, sample));
                        _output.Add(Colorize(_second[corner + 1], gradient, sample));
                    }
                }
            }

            vertices.Clear();
            vertices.AddUIVertexTriangleStream(_output);
            return true;
        }

        private void UpdateStops(in GradientSettings gradient)
        {
            if (ReferenceEquals(_cachedRamp, gradient.ramp) && _cachedRevision == gradient.rampRevision)
                return;

            _cachedRamp = gradient.ramp;
            _cachedRevision = gradient.rampRevision;
            _stops.Clear();
            _stops.Add(0);
            _stops.Add(1);
            foreach (var key in gradient.ramp.colorKeys)
                _stops.Add(key.time);
            foreach (var key in gradient.ramp.alphaKeys)
                _stops.Add(key.time);

            if (gradient.ramp.mode == GradientMode.PerceptualBlend)
                for (var index = 1; index < PERCEPTUAL_SAMPLES; index++)
                    _stops.Add(index / (float)PERCEPTUAL_SAMPLES);

            _stops.Sort();
            for (var index = _stops.Count - 1; index > 0; index--)
                if (_stops[index] == _stops[index - 1])
                    _stops.RemoveAt(index);
        }

        private static int Clip(GradientVertex[] input, int count, GradientVertex[] output, float boundary, bool keepAbove)
        {
            if (count == 0)
                return 0;

            var written = 0;
            var previous = input[count - 1];
            var previousInside = keepAbove ? previous.Position >= boundary : previous.Position <= boundary;
            for (var index = 0; index < count; index++)
            {
                var current = input[index];
                var inside = keepAbove ? current.Position >= boundary : current.Position <= boundary;
                if (inside != previousInside)
                    output[written++] = GradientVertex.Intersect(previous, current, boundary);
                if (inside)
                    output[written++] = current;
                previous = current;
                previousInside = inside;
            }
            return written;
        }

        private static UIVertex Colorize(GradientVertex source, in GradientSettings gradient, float sample)
        {
            var vertex = source.Vertex;
            var position = gradient.ramp.mode == GradientMode.Fixed ? sample : source.Position;
            vertex.color = gradient.Blend(vertex.color, gradient.ramp.Evaluate(position));
            return vertex;
        }
    }
}
