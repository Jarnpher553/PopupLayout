using SkiaSharp;
using SkiaSharp.Views.Forms;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using System;
namespace BiClubApp.Controls
{
    [ContentProperty("Content")]
    public class PopupLayout : Grid
    {
        // 包含弹出层的绝对布局
        private AbsoluteLayout _absoluteLayout = new AbsoluteLayout() { InputTransparent = true };

        // 弹出层的背景绘图层
        private SKCanvasView _canvasView = new SKCanvasView { IsVisible = false, Opacity = 1 };

        // 弹出层的背景的大小（绘气泡层需要）
        private Rectangle _rect;

        //箭头初始位置
        private double _drawX;

        //是否画气泡（保证ios、android兼容）
        private bool _isDrawTriangle;

        //箭头方向
        private bool _isArrowUp;

        // 多弹出窗列表
        public ObservableCollection<PopupItem> PopupItems { get; } = new ObservableCollection<PopupItem>();

        //区域外tap触发
        public  event EventHandler HideEvent;

        // 默认子元素属性
        public View Content
        {
            set
            {
                //插入子元素
                Children.Add(value);
                //同时插入绝对布局，保证绝对布局在子元素之后
                //图层就在子元素之上
                Children.Add(_absoluteLayout);
            }
        }

        public PopupLayout()
        {
            // 点击手势
            var tap = new TapGestureRecognizer();
            // 点击手势响应事件
            tap.Tapped += async (sender, e) =>
            {
                HideEvent?.Invoke(sender, e);
                await HidePopup();
            };
            // 绝对布局添加点击手势
            _canvasView.GestureRecognizers.Add(tap);

            // 画布定位方式（大小按比例）
            _canvasView.SetValue(AbsoluteLayout.LayoutFlagsProperty, AbsoluteLayoutFlags.SizeProportional);
            // 画布定位
            _canvasView.SetValue(AbsoluteLayout.LayoutBoundsProperty, new Rectangle(0, 0, 1, 1));

            // 将画布添加到绝对布局中
            _absoluteLayout.Children.Add(_canvasView);

            // 弹窗列表改变事件
            PopupItems.CollectionChanged += (object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) =>
            {
                var items = (ObservableCollection<PopupItem>)sender;

                foreach (var item in items)
                {
                    var view = item.Content;
                    if (!_absoluteLayout.Children.Contains(view))
                    {
                        // 设置不可见
                        view.IsVisible = false;
                        // 设置透明度0
                        view.Opacity = 1;

                        // 弹窗定位方式（绝对定位）
                        view.SetValue(AbsoluteLayout.LayoutFlagsProperty, AbsoluteLayoutFlags.None);

                        // 将弹窗添加进绝对布局
                        _absoluteLayout.Children.Add(view);
                    }
                }
            };

            // 画布绘图事件
            _canvasView.PaintSurface += (sender, e) =>
            {
                // 获取画布实例
                var canvas = e.Surface.Canvas;
                // 清除画布
                canvas.Clear();

                if (!_isDrawTriangle)
                {
                    return;
                }

                // 计算画布上的画图区域
                var rect = new Rectangle(_rect.X / Width * e.Info.Width,
                   _rect.Y / Height * e.Info.Height,
                   _rect.Width / Width * e.Info.Width,
                   _rect.Height / Height * e.Info.Height);

                _drawX = _drawX / Width * e.Info.Width;

                // 实例化刷子
                var paint = new SKPaint { Style = SKPaintStyle.StrokeAndFill, Color = SKColors.Gainsboro, IsAntialias = true };

                // 实例化路径
                var path = new SKPath();

                // 箭头方向向上
                if (_isArrowUp)
                {
                    // 绘制路径
                    path.MoveTo((float)(_drawX + 15), (float)(rect.Y));
                    path.LineTo((float)(_drawX + 30), (float)(rect.Y - 20));
                    path.LineTo((float)(_drawX + 45), (float)(rect.Y));
                    path.LineTo((float)(rect.X + rect.Width), (float)(rect.Y));
                    path.LineTo((float)(rect.X + rect.Width), (float)(rect.Y + rect.Height));
                    path.LineTo((float)(rect.X), (float)(rect.Y + rect.Height));
                    path.LineTo((float)(rect.X), (float)(rect.Y));
                    path.Close();
                }
                else
                {
                    // 绘制路径
                    path.MoveTo((float)(_drawX + 15), (float)(rect.Y + rect.Height));
                    path.LineTo((float)(_drawX + 30), (float)(rect.Y + rect.Height + 20));
                    path.LineTo((float)(_drawX + 45), (float)(rect.Y + rect.Height));
                    path.LineTo((float)(rect.X + rect.Width), (float)(rect.Y + rect.Height));
                    path.LineTo((float)(rect.X + rect.Width), (float)(rect.Y));
                    path.LineTo((float)(rect.X), (float)(rect.Y));
                    path.LineTo((float)(rect.X), (float)(rect.Y + rect.Height));
                    path.Close();
                }

                // 按路径绘图
                canvas.DrawPath(path, paint);
            };
        }


        /// <summary>
        /// 弹出
        /// </summary>
        /// <param name="popupItem">窗体实例</param>
        /// <param name="rect">矩形区域</param>
        /// <returns>Task</returns>
        private async Task ShowPopup(PopupItem popupItem, Rectangle rect)
        {
            //重绘
            _rect = rect;
            _canvasView.InvalidateSurface();

            // 响应不传递
            _absoluteLayout.InputTransparent = false;

            // 画布可见
            _canvasView.IsVisible = true;

            // 获取弹窗的view对象
            var popup = ((AbsoluteLayout)Children[1]).Children.First(c => c == popupItem.Content);

            // 向内偏移2
            var localRect = new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4);
            // 设置弹窗位置
            popup.SetValue(AbsoluteLayout.LayoutBoundsProperty, localRect);

            // 弹窗对象可见
            //popupItem.IsShown = true;

            // 弹窗view可见
            popup.IsVisible = true;

            // 显示动画
            await Task.WhenAll(
              popup.FadeTo(1, 200, Easing.CubicInOut),
              _canvasView.FadeTo(1, 200, Easing.CubicInOut)
            ).ConfigureAwait(false);
        }

        /// <summary>
        /// 弹出
        /// </summary>
        /// <param name="popupItem">窗体实例</param>
        /// <param name="clickElem">被点击元素</param>
        /// <param name="width">弹窗宽度</param>
        /// <param name="height">弹窗高度</param>
        /// <returns></returns>
        public async Task ShowPopup(PopupItem popupItem, VisualElement clickElem, double width = 100, double height = 200, bool isProportional = false, bool isDrawTriangle = false)
        {
            if (isProportional)
            {
                width = width > 1 ? 1 : width;
                height = height > 1 ? 1 : height;
            }

            _isDrawTriangle = isDrawTriangle;

            // 获取点击元素相对X
            var x = clickElem.X;
            // 获取点击元素相对Y
            var y = clickElem.Y;
            // 获取点击元素高
            var h = clickElem.Height;

            // 获取点击元素父元素
            var parent = clickElem.Parent as VisualElement;
            // 如果父元素不是this布局对象
            while (parent != this)
            {
                // X加上父元素的相对偏移量
                x += parent.X;
                // Y加上父元素的相对偏移量
                y += parent.Y;
                // 获取父元素的父元素
                parent = parent.Parent as VisualElement;
            }


            _drawX = x;

            // 加上元素高度后为弹窗起始y
            if (Children[0].GetType() == typeof(ScrollView))
            {
                var scrollView = Children[0] as ScrollView;
                var scrollY = scrollView.ScrollY;
                y = y + h - scrollY;
            }
            else
            {
                y = y + h;
            }

            if (isProportional)
            {
                width = Width * width;

                if ((y - h / 2) < (Height / 2))
                {
                    height = (Height - y) * height;

                }
                else
                {
                    height = (y - h) * height;
                }
            }

            // 弹窗宽度大于屏幕宽度
            if (width > Width)
            {
                // 起始x位置设置为0
                x = 0;
                // 宽度设置为屏幕宽度
                width = Width;
            }
            else
            {
                // 如果x加上弹窗宽度超出屏幕
                if (x + width > Width)
                {
                    x = Width - width;
                }
            }

            //var test = absoluteLayout.Height;
            // 点击元素位置在屏幕位置一半以上
            if ((y - h / 2) < (Height / 2))
            {
                // 弹窗高度大于下方剩余屏幕高度
                if (height > Height - y)
                {
                    height = Height - y;
                }

                // 箭头向上
                _isArrowUp = true;
            }
            else
            {
                // 弹窗高度大于上方剩余屏幕高度
                if (height > y - h)
                {
                    height = y - h;
                }
                // 设置y起始位置为点击元素起始位置往上height高度处
                y = y - h - height;

                // 箭头向下
                _isArrowUp = false;
            }

            // 当获取到绝对X、Y后，弹窗
            await ShowPopup(popupItem, new Rectangle(x, y, width, height));
        }

        /// <summary>
        /// 隐藏窗体
        /// </summary>
        /// <returns></returns>
        public async Task HidePopup()
        {
            // 绝对布局响应传递
            _absoluteLayout.InputTransparent = true;

            // 获取绝对布局的所有子元素
            var children = ((AbsoluteLayout)Children[1]).Children.ToList();

            // 设置所有子元素不可见
            children.ForEach(d => d.IsVisible = false);

            // 等待所有子元素的渐出动画
            await Task.WhenAll(children.Select(d => d.FadeTo(0, 200, Easing.CubicInOut)))
                .ConfigureAwait(false);

            // 设置所有弹窗对象不可见（没用）
            //PopupItems.ToList().ForEach(d => d.IsShown = false);
        }

        public async Task PopUpOverBelow(object visualElement, PopupItem popupItem, float width, float height) {
            _absoluteLayout.InputTransparent = false;

            var popup = ((AbsoluteLayout)Children[1]).Children.First(c => c == popupItem.Content);

            var v = visualElement as VisualElement;
            var x = v.X;
            var y = v.Y;
            var w = v.Width;
            var h = v.Height;
            var ifPop = false;
            while (true) {
                v = v.Parent as VisualElement;
                if (v != null) {
                    x += v.X;
                    y += v.Y;
                    if (v == this) {
                        ifPop = true;
                        break;
                    }
                } else
                    break;
            }

            if (ifPop) {
                popup.SetValue(AbsoluteLayout.LayoutBoundsProperty, new Rectangle(x, y + h, width, height));

                //popupItem.IsShown = true;

                popup.IsVisible = true;

                await Task.WhenAll(
                  popup.FadeTo(1, 200, Easing.CubicInOut)
                );
            }
        }

    }

    [ContentProperty("Content")]
    public class PopupItem
    {
        public View Content
        {
            get; set;
        }
    }
}
