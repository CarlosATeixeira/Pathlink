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
using System.Windows.Shapes;

namespace Pathlink
{
    /// <summary>
    /// Interaction logic for ObstructionDialog.xaml
    /// </summary>
    public partial class ObstructionDialog : Window
    {
        public string SelectedObstructionType { get; private set; }
        public double ObstructionHeight { get; private set; }
        public int ObstructionGrowthMargin { get; private set; }

        public ObstructionDialog()
        {
            InitializeComponent();
            DataObject.AddPastingHandler(ObstructionHeightTextBox, OnPaste);
            DataObject.AddPastingHandler(ObstructionGrowthMarginTextBox, OnPaste);
        }

        private void InsertButton_Click(object sender, RoutedEventArgs e)
        {
            if (ObstructionTypeComboBox.SelectedItem != null && ObstructionHeightTextBox.Text != "" &&  ObstructionGrowthMarginTextBox.Text != "")
            {
                SelectedObstructionType = ((ComboBoxItem)ObstructionTypeComboBox.SelectedItem).Content.ToString();
                ObstructionHeight = double.Parse(ObstructionHeightTextBox.Text);
                ObstructionGrowthMargin = int.Parse(ObstructionGrowthMarginTextBox.Text);
                this.DialogResult = true;
            }
            else
            {
                // Trate o caso em que nenhum item está selecionado, ou defina um valor padrão para SelectedObstructionType
                SelectedObstructionType = "Valor padrão ou tratamento para nenhuma seleção";
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Expressão regular que corresponde apenas a números
            var regex = new System.Text.RegularExpressions.Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));
                var regex = new System.Text.RegularExpressions.Regex("[^0-9]+");
                if (regex.IsMatch(text))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }


    }

}
