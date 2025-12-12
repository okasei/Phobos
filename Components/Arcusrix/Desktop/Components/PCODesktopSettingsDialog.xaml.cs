using System;
using System.Windows;

namespace Phobos.Components.Arcusrix.Desktop.Components
{
    /// <summary>
    /// PCODesktopSettingsDialog.xaml 的交互逻辑
    /// </summary>
    public partial class PCODesktopSettingsDialog : Window
    {
        private int _columns;
        private int _rows;
        private bool _shouldResetLayout = false;

        public int Columns => _columns;
        public int Rows => _rows;
        public bool ShouldResetLayout => _shouldResetLayout;

        public PCODesktopSettingsDialog(int currentColumns, int currentRows)
        {
            InitializeComponent();

            _columns = currentColumns;
            _rows = currentRows;

            ColumnsText.Text = _columns.ToString();
            RowsText.Text = _rows.ToString();
        }

        private void IncreaseColumns_Click(object sender, RoutedEventArgs e)
        {
            if (_columns < 20)
            {
                _columns++;
                ColumnsText.Text = _columns.ToString();
            }
        }

        private void DecreaseColumns_Click(object sender, RoutedEventArgs e)
        {
            if (_columns > 3)
            {
                _columns--;
                ColumnsText.Text = _columns.ToString();
            }
        }

        private void IncreaseRows_Click(object sender, RoutedEventArgs e)
        {
            if (_rows < 20)
            {
                _rows++;
                RowsText.Text = _rows.ToString();
            }
        }

        private void DecreaseRows_Click(object sender, RoutedEventArgs e)
        {
            if (_rows > 2)
            {
                _rows--;
                RowsText.Text = _rows.ToString();
            }
        }

        private void ResetLayout_Click(object sender, RoutedEventArgs e)
        {
            var result = Service.Arcusrix.PSDialogService.Confirm(
                "Are you sure you want to reset the desktop layout?\n\nAll folders will be removed and plugins will be rearranged in default order.",
                "Confirm Reset",
                true,
                this);

            if (result)
            {
                _shouldResetLayout = true;
                DialogResult = true;
                Close();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// 显示设置对话框
        /// </summary>
        public static (bool saved, int columns, int rows, bool resetLayout) Show(Window owner, int currentColumns, int currentRows)
        {
            var dialog = new PCODesktopSettingsDialog(currentColumns, currentRows)
            {
                Owner = owner
            };

            var result = dialog.ShowDialog() == true;

            return (result, dialog.Columns, dialog.Rows, dialog.ShouldResetLayout);
        }
    }
}
