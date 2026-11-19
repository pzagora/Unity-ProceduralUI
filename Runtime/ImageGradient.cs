using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProceduralUI
{
    public sealed class ImageGradient
    {
        private readonly GradientSettingsAccessor _settings;
        private readonly Action _changed;

        internal ref GradientSettings Settings => ref _settings();

        public bool Enabled
        {
            get => Settings.enabled;
            set => SetValue(ref Settings.enabled, value);
        }

        public GradientType Type
        {
            get => Settings.type;
            set => SetValue(ref Settings.type, value);
        }

        public GradientBlendMode BlendMode
        {
            get => Settings.blendMode;
            set => SetValue(ref Settings.blendMode, value);
        }

        public float Intensity
        {
            get => Settings.intensity;
            set => SetValue(ref Settings.intensity, Mathf.Clamp01(value));
        }

        public float Angle
        {
            get => Settings.angle;
            set => SetValue(ref Settings.angle, Mathf.Repeat(value, 360f));
        }

        public Color StartColor
        {
            get => Settings.startColor;
            set => SetValue(ref Settings.startColor, value);
        }

        public Color EndColor
        {
            get => Settings.endColor;
            set => SetValue(ref Settings.endColor, value);
        }

        public Color TopLeft
        {
            get => Settings.topLeft;
            set => SetValue(ref Settings.topLeft, value);
        }

        public Color TopRight
        {
            get => Settings.topRight;
            set => SetValue(ref Settings.topRight, value);
        }

        public Color BottomLeft
        {
            get => Settings.bottomLeft;
            set => SetValue(ref Settings.bottomLeft, value);
        }

        public Color BottomRight
        {
            get => Settings.bottomRight;
            set => SetValue(ref Settings.bottomRight, value);
        }

        internal ImageGradient(GradientSettingsAccessor settings, Action changed)
        {
            _settings = settings;
            _changed = changed;
        }

        public void SetRamp(Gradient value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            ref var settings = ref Settings;
            settings.ramp ??= new Gradient();
            settings.ramp.SetKeys(value.colorKeys, value.alphaKeys);
            settings.ramp.mode = value.mode;
            settings.ramp.colorSpace = value.colorSpace;
            settings.InvalidateRamp();
            _changed();
        }

        public Gradient CopyRamp()
        {
            var ramp = Settings.ramp;
            return new Gradient { colorKeys = ramp.colorKeys, alphaKeys = ramp.alphaKeys, mode = ramp.mode, colorSpace = ramp.colorSpace };
        }

        private void SetValue<T>(ref T field, T value)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;

            field = value;
            _changed();
        }
    }
}
