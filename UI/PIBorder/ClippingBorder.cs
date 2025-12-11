using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

namespace Phobos.UI.PIBorder
{/// <summary>
 /// ClippingBorder
 /// - 每角不同半径（StreamGeometry）
 /// - 高性能：复用 geometry、最小化更新、可选 BitmapCache
 /// - DPI 适配：WM_DPICHANGED + GetDpi polling（兼容各种 WPF 版本）
 /// - 去抖合并：在短时间窗口内合并多次触发
 /// </summary>
    public class ClippingBorder : Border
    {
        private readonly StreamGeometry _clipGeometry = new StreamGeometry();
        private object _oldClip;

        private Size _lastSize = Size.Empty;
        private Thickness _lastBorderThickness = new Thickness(-1);
        private CornerRadius _lastCornerRadius = new CornerRadius(-1);
        private DpiScale _lastDpi;
        private HwndSource _hwndSource;
        private bool _hookedDpiMessage;

        // 去抖：在短时间窗口内合并多次请求
        private readonly DispatcherTimer _coalesceTimer;
        private bool _needApply = false;

        public static readonly DependencyProperty EnableBitmapCacheProperty =
            DependencyProperty.Register(nameof(EnableBitmapCache), typeof(bool), typeof(ClippingBorder),
                new PropertyMetadata(false, OnEnableBitmapCacheChanged));

        public bool EnableBitmapCache
        {
            get => (bool)GetValue(EnableBitmapCacheProperty);
            set => SetValue(EnableBitmapCacheProperty, value);
        }

        private static void OnEnableBitmapCacheChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ClippingBorder cb)
                cb.UpdateChildCacheMode();
        }

        public ClippingBorder()
        {
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
            SizeChanged += (s, e) => RequestApplyClip();
            LayoutUpdated += (s, e) => RequestApplyClip();

            // coalesce timer: 40ms 窗口（可调），将多次触发合并为一次真实更新
            _coalesceTimer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = TimeSpan.FromMilliseconds(40)
            };
            _coalesceTimer.Tick += (s, e) =>
            {
                _coalesceTimer.Stop();
                if (_needApply)
                {
                    _needApply = false;
                    ApplyClip(force: true);
                }
            };
        }

        public override UIElement Child
        {
            get => base.Child;
            set
            {
                if (base.Child == value) return;

                // 恢复旧 child 的 Clip & CacheMode
                if (base.Child != null)
                {
                    base.Child.SetValue(UIElement.ClipProperty, _oldClip);
                    if (EnableBitmapCache && base.Child is UIElement oldChild)
                        oldChild.CacheMode = null;
                }

                // 保存即将设置 child 的原本地 Clip 以便恢复
                _oldClip = value?.ReadLocalValue(UIElement.ClipProperty);
                base.Child = value;

                UpdateChildCacheMode();
                RequestApplyClip();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 初始化 DPI snapshot
            _lastDpi = VisualTreeHelper.GetDpi(this);

            // 尝试 Hook WM_DPICHANGED（兼容全部版本）
            if (!_hookedDpiMessage)
            {
                _hwndSource = PresentationSource.FromVisual(this) as HwndSource;
                if (_hwndSource != null)
                {
                    _hwndSource.AddHook(HwndMessageHook);
                    _hookedDpiMessage = true;
                }
            }

            // 初次应用
            RequestApplyClip();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // 恢复 child 原 Clip 与 Cache
            if (base.Child != null)
            {
                base.Child.SetValue(UIElement.ClipProperty, _oldClip);
                if (EnableBitmapCache && base.Child is UIElement child)
                    child.CacheMode = null;
            }

            if (_hwndSource != null && _hookedDpiMessage)
            {
                _hwndSource.RemoveHook(HwndMessageHook);
                _hwndSource = null;
                _hookedDpiMessage = false;
            }

            _coalesceTimer.Stop();
            _needApply = false;
        }

        private IntPtr HwndMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_DPICHANGED = 0x02E0;
            if (msg == WM_DPICHANGED)
            {
                // Windows 通知 DPI 变更 —— 仅触发一次合并更新（去抖）
                var dpi = VisualTreeHelper.GetDpi(this);
                if (!DpiEquals(dpi, _lastDpi))
                {
                    _lastDpi = dpi;
                    RequestApplyClip();
                }
            }
            return IntPtr.Zero;
        }

        private static bool DpiEquals(DpiScale a, DpiScale b)
        {
            return Math.Abs(a.DpiScaleX - b.DpiScaleX) < 0.0001
                   && Math.Abs(a.DpiScaleY - b.DpiScaleY) < 0.0001;
        }

        /// <summary>
        /// 请求应用 Clip（会被合并到 coalesce timer）
        /// </summary>
        private void RequestApplyClip()
        {
            _needApply = true;
            // 重启去抖计时：40ms 内的所有请求合并
            _coalesceTimer.Stop();
            _coalesceTimer.Start();
        }

        /// <summary>
        /// 仅在真正需要时重算（根据 RenderSize / BorderThickness / CornerRadius / DPI）
        /// </summary>
        private void InvalidateClipIfNeeded()
        {
            var size = RenderSize;
            var border = BorderThickness;
            var corner = CornerRadius;
            var dpi = VisualTreeHelper.GetDpi(this);

            bool sizeChanged = !size.Equals(_lastSize);
            bool borderChanged = !border.Equals(_lastBorderThickness);
            bool cornerChanged = !corner.Equals(_lastCornerRadius);
            bool dpiChanged = !DpiEquals(dpi, _lastDpi);

            if (sizeChanged || borderChanged || cornerChanged || dpiChanged)
            {
                _lastSize = size;
                _lastBorderThickness = border;
                _lastCornerRadius = corner;
                _lastDpi = dpi;
                RequestApplyClip();
            }
        }

        /// <summary>
        /// 核心：使用 StreamGeometry 按每角分别绘制圆角并复用 geometry 实例。
        /// </summary>
        protected virtual void ApplyClip(bool force = false)
        {
            // 在执行前再检查一次（避免在动画非常早期 render size 为 0）
            InvalidateClipIfNeeded();

            UIElement child = base.Child;
            if (child == null)
            {
                if (force)
                {
                    // 清空 geometry
                    using (var ctx = _clipGeometry.Open()) { }
                }
                return;
            }

            if (RenderSize.Width <= 0 || RenderSize.Height <= 0)
                return;

            // 内部内容区域（把裁剪放在 border 内部）
            double left = BorderThickness.Left / 2.0;
            double top = BorderThickness.Top / 2.0;
            double width = Math.Max(0.0, RenderSize.Width - BorderThickness.Left - BorderThickness.Right);
            double height = Math.Max(0.0, RenderSize.Height - BorderThickness.Top - BorderThickness.Bottom);

            Rect rect = new Rect(left, top, width, height);

            // 每角半径（以 CornerRadius 的各向量为基准，并减去边框一半的影响）
            double tl = Math.Max(0.0, CornerRadius.TopLeft - (BorderThickness.Left * 0.5));
            double tr = Math.Max(0.0, CornerRadius.TopRight - (BorderThickness.Right * 0.5));
            double br = Math.Max(0.0, CornerRadius.BottomRight - (BorderThickness.Right * 0.5));
            double bl = Math.Max(0.0, CornerRadius.BottomLeft - (BorderThickness.Left * 0.5));

            // 约束半径（避免超过尺寸）
            ConstrainCornerRadii(ref tl, ref tr, ref br, ref bl, rect.Width, rect.Height);

            // 判断是否真的需要更新 geometry
            bool sizeChanged = !_lastSize.Equals(RenderSize);
            bool borderChanged = !_lastBorderThickness.Equals(BorderThickness);
            bool cornerChanged = !_lastCornerRadius.Equals(CornerRadius);
            bool dpiChanged = !_lastDpi.Equals(VisualTreeHelper.GetDpi(this)); // 注意：这里只是粗略比较
            bool needUpdate = force || sizeChanged || borderChanged || cornerChanged || dpiChanged;

            if (!needUpdate)
                return;

            // 重写 geometry 路径（复用 _clipGeometry）
            using (var ctx = _clipGeometry.Open())
            {
                // start at top-left corner (offset by tl)
                ctx.BeginFigure(new Point(rect.X + tl, rect.Y), isFilled: true, isClosed: true);

                // top edge -> top-right corner start
                ctx.LineTo(new Point(rect.X + rect.Width - tr, rect.Y), isStroked: true, isSmoothJoin: false);

                // top-right arc
                if (tr > 0)
                    ctx.ArcTo(new Point(rect.X + rect.Width, rect.Y + tr),
                              new Size(tr, tr), rotationAngle: 0, isLargeArc: false, SweepDirection.Clockwise, isStroked: true, isSmoothJoin: false);
                else
                    ctx.LineTo(new Point(rect.X + rect.Width, rect.Y), isStroked: true, isSmoothJoin: false);

                // right edge -> bottom-right corner start
                ctx.LineTo(new Point(rect.X + rect.Width, rect.Y + rect.Height - br), isStroked: true, isSmoothJoin: false);

                // bottom-right arc
                if (br > 0)
                    ctx.ArcTo(new Point(rect.X + rect.Width - br, rect.Y + rect.Height),
                              new Size(br, br), rotationAngle: 0, isLargeArc: false, SweepDirection.Clockwise, isStroked: true, isSmoothJoin: false);
                else
                    ctx.LineTo(new Point(rect.X + rect.Width, rect.Y + rect.Height), isStroked: true, isSmoothJoin: false);

                // bottom edge -> bottom-left corner start
                ctx.LineTo(new Point(rect.X + bl, rect.Y + rect.Height), isStroked: true, isSmoothJoin: false);

                // bottom-left arc
                if (bl > 0)
                    ctx.ArcTo(new Point(rect.X, rect.Y + rect.Height - bl),
                              new Size(bl, bl), rotationAngle: 0, isLargeArc: false, SweepDirection.Clockwise, isStroked: true, isSmoothJoin: false);
                else
                    ctx.LineTo(new Point(rect.X, rect.Y + rect.Height), isStroked: true, isSmoothJoin: false);

                // left edge -> top-left corner start
                ctx.LineTo(new Point(rect.X, rect.Y + tl), isStroked: true, isSmoothJoin: false);

                // top-left arc
                if (tl > 0)
                    ctx.ArcTo(new Point(rect.X + tl, rect.Y),
                              new Size(tl, tl), rotationAngle: 0, isLargeArc: false, SweepDirection.Clockwise, isStroked: true, isSmoothJoin: false);
                else
                    ctx.LineTo(new Point(rect.X, rect.Y), isStroked: true, isSmoothJoin: false);
            }

            // 仅当 child 没有本地 Clip（或已经是我们 geometry）时才设置
            var localValue = child.ReadLocalValue(UIElement.ClipProperty);
            if (localValue == DependencyProperty.UnsetValue || ReferenceEquals(localValue, _clipGeometry))
            {
                child.Clip = _clipGeometry;
            }

            // 更新 last 状态
            _lastSize = RenderSize;
            _lastBorderThickness = BorderThickness;
            _lastCornerRadius = CornerRadius;
            _lastDpi = VisualTreeHelper.GetDpi(this);
        }

        private static void ConstrainCornerRadii(ref double tl, ref double tr, ref double br, ref double bl, double width, double height)
        {
            // 按 WPF 规则：每条边上两角之和不得超过边长
            double topSum = tl + tr;
            if (topSum > width && topSum > 0)
            {
                double scale = width / topSum;
                tl *= scale;
                tr *= scale;
            }

            double bottomSum = bl + br;
            if (bottomSum > width && bottomSum > 0)
            {
                double scale = width / bottomSum;
                bl *= scale;
                br *= scale;
            }

            double leftSum = tl + bl;
            if (leftSum > height && leftSum > 0)
            {
                double scale = height / leftSum;
                tl *= scale;
                bl *= scale;
            }

            double rightSum = tr + br;
            if (rightSum > height && rightSum > 0)
            {
                double scale = height / rightSum;
                tr *= scale;
                br *= scale;
            }
        }

        private void UpdateChildCacheMode()
        {
            if (base.Child is UIElement child)
            {
                if (EnableBitmapCache)
                {
                    child.CacheMode = new BitmapCache();
                }
                else
                {
                    child.CacheMode = null;
                }
            }
        }
    }
}