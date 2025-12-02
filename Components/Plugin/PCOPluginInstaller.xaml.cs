using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Phobos.Components.Plugin
{
    /// <summary>
    /// PCOPluginInstaller.xaml 的交互逻辑
    /// </summary>
    public partial class PCOPluginInstaller : Window
    {
        public PCOPluginInstaller()
        {
        }
        /// <summary>
         /// 设置插件信息
         /// </summary>
         /// <param name="pluginInfo">插件信息对象</param>
        public void SetPluginInfo(PluginInfo pluginInfo)
        {
            if (pluginInfo == null) return;

            PluginNameText.Text = pluginInfo.Name ?? "未知插件";
            PluginVersionText.Text = pluginInfo.Version ?? "v1.0.0";
            PluginManufacturerText.Text = pluginInfo.Manufacturer ?? "未知制造商";
            PluginDescriptionText.Text = pluginInfo.Description ?? "暂无描述";

            if (!string.IsNullOrEmpty(pluginInfo.IconPath))
            {
                try
                {
                    PluginIcon.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(pluginInfo.IconPath));
                }
                catch
                {
                    // 图标加载失败，保持默认
                }
            }

            if (!string.IsNullOrEmpty(pluginInfo.BannerPath))
            {
                try
                {
                    BannerImage.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(pluginInfo.BannerPath));
                }
                catch
                {
                    // Banner加载失败，保持默认
                }
            }
        }

        /// <summary>
        /// 标题栏拖动
        /// </summary>
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                DragMove();
            }
        }

        /// <summary>
        /// 最小化按钮点击
        /// </summary>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// 关闭按钮点击
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 退出按钮点击
        /// </summary>
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// 安装按钮点击
        /// </summary>
        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            OnInstallRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 安装请求事件
        /// </summary>
        public event EventHandler OnInstallRequested;
    }

    /// <summary>
    /// 插件信息模型
    /// </summary>
    public class PluginInfo
    {
        /// <summary>
        /// 插件名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 插件版本
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 制造商
        /// </summary>
        public string Manufacturer { get; set; }

        /// <summary>
        /// 插件描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 插件图标路径
        /// </summary>
        public string IconPath { get; set; }

        /// <summary>
        /// Banner图片路径
        /// </summary>
        public string BannerPath { get; set; }

        /// <summary>
        /// 插件安装包路径
        /// </summary>
        public string PackagePath { get; set; }
    }
}
