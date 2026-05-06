using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPF_AdminFelulet.Views
{
    /// <summary>
    /// Interaction logic for EtelekUserControl.xaml
    /// </summary>
    public partial class EtelekUserControl : UserControl
    {
        public EtelekUserControl()
        {
            InitializeComponent();
        }

        private void NumberOnlyTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void NumberOnlyTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            var pastedText = e.DataObject.GetData(DataFormats.Text) as string;
            if (string.IsNullOrWhiteSpace(pastedText) || !pastedText.All(char.IsDigit))
            {
                e.CancelCommand();
            }
        }
    }
}
