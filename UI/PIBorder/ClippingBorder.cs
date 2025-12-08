using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Phobos.UI.PIBorder
{
    public class ClippingBorder : System.Windows.Controls.Border
    {
        private RectangleGeometry _clipRect = new RectangleGeometry();
        private object? _oldClip;

        protected override void OnRender(DrawingContext dc)
        {
            OnApplyChildClip();
            base.OnRender(dc);
        }

        public override UIElement Child
        {
            get { return base.Child; }
            set
            {
                if (this.Child != value)
                {
                    // 恢复原有Clip
                    if (this.Child != null)
                    {
                        this.Child.SetValue(UIElement.ClipProperty, _oldClip);
                    }
                    if (value != null)
                    {
                        _oldClip = value.ReadLocalValue(UIElement.ClipProperty);
                    }
                    else
                    {
                        _oldClip = null;
                    }
                    base.Child = value;
                }
            }
        }

        // 核心方法：动态计算并应用裁剪区域
        protected virtual void OnApplyChildClip()
        {
            UIElement child = this.Child;
            if (child != null)
            {
                // 考虑边框厚度，计算内部子元素应有的裁剪区域
                _clipRect.RadiusX = _clipRect.RadiusY = Math.Max(0.0, this.CornerRadius.TopLeft - (this.BorderThickness.Left * 0.5));
                Rect rect = new Rect(this.RenderSize);
                rect.Height -= (this.BorderThickness.Top + this.BorderThickness.Bottom);
                rect.Width -= (this.BorderThickness.Left + this.BorderThickness.Right);
                _clipRect.Rect = rect;
                child.Clip = _clipRect;
            }
        }
    }
}
