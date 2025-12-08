using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Phobos.Components.Arcusrix.Menu;

namespace Phobos.Manager.Arcusrix
{
    /// <summary>
    /// 菜单管理器 - 提供全局菜单创建和管理功能
    /// </summary>
    public class PMMenu
    {
        private static PMMenu? _instance;
        private static readonly object _lock = new();

        /// <summary>
        /// 单例实例
        /// </summary>
        public static PMMenu Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new PMMenu();
                    }
                }
                return _instance;
            }
        }

        private PMMenu() { }

        #region 菜单项构建

        /// <summary>
        /// 创建菜单项
        /// </summary>
        public PhobosMenuItem CreateItem(string id, string text, string icon = "", Action? onClick = null, bool isDanger = false)
        {
            return new PhobosMenuItem
            {
                Id = id,
                Text = text,
                Icon = icon,
                OnClick = onClick,
                IsDanger = isDanger
            };
        }

        /// <summary>
        /// 创建带本地化的菜单项
        /// </summary>
        public PhobosMenuItem CreateLocalizedItem(
            string id,
            Dictionary<string, string> localizedTexts,
            string icon = "",
            Action? onClick = null,
            bool isDanger = false)
        {
            return new PhobosMenuItem
            {
                Id = id,
                LocalizedTexts = localizedTexts,
                Icon = icon,
                OnClick = onClick,
                IsDanger = isDanger
            };
        }

        /// <summary>
        /// 创建分隔符
        /// </summary>
        public PhobosMenuItem CreateSeparator()
        {
            return PhobosMenuItem.Separator();
        }

        /// <summary>
        /// 创建带子菜单的菜单项
        /// </summary>
        public PhobosMenuItem CreateSubmenuItem(string id, string text, string icon, IEnumerable<PhobosMenuItem> children)
        {
            return new PhobosMenuItem
            {
                Id = id,
                Text = text,
                Icon = icon,
                Children = new List<PhobosMenuItem>(children)
            };
        }

        #endregion

        #region 菜单显示

        /// <summary>
        /// 在指定位置显示菜单
        /// </summary>
        /// <param name="container">菜单容器（通常是 Grid 或 Panel）</param>
        /// <param name="items">菜单项列表</param>
        /// <param name="position">相对于容器的位置</param>
        /// <param name="onItemSelected">菜单项选中回调</param>
        /// <returns>创建的菜单控件</returns>
        public PCOMenu ShowAt(
            Panel container,
            IEnumerable<PhobosMenuItem> items,
            Point position,
            Action<PhobosMenuItem>? onItemSelected = null)
        {
            // 关闭已存在的菜单
            CloseMenusIn(container);

            var menu = new PCOMenu();
            container.Children.Add(menu);

            if (onItemSelected != null)
            {
                menu.ItemSelected += (s, e) => onItemSelected(e.SelectedItem);
            }

            menu.Show(items, position);
            return menu;
        }

        /// <summary>
        /// 在元素旁边显示菜单
        /// </summary>
        /// <param name="container">菜单容器</param>
        /// <param name="items">菜单项列表</param>
        /// <param name="target">目标元素</param>
        /// <param name="onItemSelected">菜单项选中回调</param>
        /// <returns>创建的菜单控件</returns>
        public PCOMenu ShowNear(
            Panel container,
            IEnumerable<PhobosMenuItem> items,
            FrameworkElement target,
            Action<PhobosMenuItem>? onItemSelected = null)
        {
            // 关闭已存在的菜单
            CloseMenusIn(container);

            var menu = new PCOMenu();
            container.Children.Add(menu);

            if (onItemSelected != null)
            {
                menu.ItemSelected += (s, e) => onItemSelected(e.SelectedItem);
            }

            menu.ShowAt(items, target);
            return menu;
        }

        /// <summary>
        /// 在鼠标位置显示菜单
        /// </summary>
        public PCOMenu ShowAtMouse(
            Panel container,
            IEnumerable<PhobosMenuItem> items,
            Action<PhobosMenuItem>? onItemSelected = null)
        {
            var mousePos = System.Windows.Input.Mouse.GetPosition(container);
            return ShowAt(container, items, mousePos, onItemSelected);
        }

        /// <summary>
        /// 关闭容器中的所有菜单
        /// </summary>
        public void CloseMenusIn(Panel container)
        {
            var menusToRemove = new List<PCOMenu>();
            foreach (var child in container.Children)
            {
                if (child is PCOMenu menu)
                {
                    menusToRemove.Add(menu);
                }
            }

            foreach (var menu in menusToRemove)
            {
                menu.Close();
                container.Children.Remove(menu);
            }
        }

        #endregion

        #region 快捷方法

        /// <summary>
        /// 显示简单的确认菜单
        /// </summary>
        public PCOMenu ShowConfirmMenu(
            Panel container,
            Point position,
            string confirmText,
            string cancelText,
            Action? onConfirm,
            Action? onCancel = null)
        {
            var items = new List<PhobosMenuItem>
            {
                CreateItem("confirm", confirmText, "✓", onConfirm),
                CreateItem("cancel", cancelText, "✗", onCancel)
            };

            return ShowAt(container, items, position);
        }

        /// <summary>
        /// 显示是/否菜单
        /// </summary>
        public PCOMenu ShowYesNoMenu(
            Panel container,
            Point position,
            Action? onYes,
            Action? onNo = null)
        {
            var items = new List<PhobosMenuItem>
            {
                CreateLocalizedItem("yes", new Dictionary<string, string>
                {
                    { "en-US", "Yes" },
                    { "zh-CN", "是" },
                    { "ja-JP", "はい" }
                }, "✓", onYes),
                CreateLocalizedItem("no", new Dictionary<string, string>
                {
                    { "en-US", "No" },
                    { "zh-CN", "否" },
                    { "ja-JP", "いいえ" }
                }, "✗", onNo)
            };

            return ShowAt(container, items, position);
        }

        #endregion
    }
}