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
using System.Configuration;
using System.Text.RegularExpressions;


namespace RebelCmsGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        CodeGenerator codeGenerator;
        public MainWindow()
        {
            InitializeComponent();
            var connectionString = ConfigurationManager.ConnectionStrings["ApplicationServices"].ConnectionString;

            codeGenerator = new(connectionString);
            GetSourceCodeType();
            GetTableName();
            GetModule();

           
        }
        private void GetTableName()
        {
            List<string> tables = codeGenerator.GetTableList();
            foreach(string table in tables)
            {
                TableNameComboBox.Items.Add(table); 
            }
        }
        private void GetModule()
        {
            var items = new[] {
    new { Text = "Setting", Value = "Setting" },
    new { Text = "Administrator", Value = "Administrator" },
    new { Text = "Menu", Value = "Menu" },
    new { Text = "Application", Value="Application" },
    new { Text = "Custom Report", Value = "Report" }
};
            ModuleComboBox.DisplayMemberPath = "Text";
            ModuleComboBox.SelectedValuePath = "Value";
            ModuleComboBox.ItemsSource = items;
        }
        private void GetSourceCodeType()
        {

            SourceCodeTypeComboBox.Items.Add(new ComboboxItem() { Text = "Please Choose", Value = "" });
            SourceCodeTypeComboBox.Items.Add(new ComboboxItem() { Text = "Pages", Value = 1 });
            SourceCodeTypeComboBox.Items.Add(new ComboboxItem() { Text = "Model", Value = 2 });
            SourceCodeTypeComboBox.Items.Add(new ComboboxItem() { Text = "Repository", Value = 3 });
            SourceCodeTypeComboBox.Items.Add(new ComboboxItem() { Text = "Controller", Value = 4 });

            SourceCodeTypeComboBox.DisplayMemberPath = "Text";
            SourceCodeTypeComboBox.SelectedValuePath = "Value";
            SourceCodeTypeComboBox.Text = "Please select";
        }
        public class ComboboxItem
        {
            public string? Text { get; set; }
            public object? Value { get; set; }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            string text = string.Empty;
            string tableName = (string)TableNameComboBox.SelectedValue;
            var module = (string)ModuleComboBox.SelectedValue;
            switch (SourceCodeTypeComboBox.SelectedValue)
            {
                case 0:
                    MessageBox.Show("Please choose la wei");
                    break;
                case 1:
                    text = codeGenerator.GeneratePages(tableName, module);
                    break;
                case 2:
                    text = codeGenerator.GenerateModel(tableName, module);
                    break;
                case 3:
                    text = codeGenerator.GenerateRepository(tableName, module);
                    break;
                case 4:
                    text = codeGenerator.GenerateController(tableName, module);
                    break;

            }
            if (!string.IsNullOrEmpty(text))
            {
                SourceCodeOutputTextBox.Text = text;
            }
        }

        private void ModuleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
