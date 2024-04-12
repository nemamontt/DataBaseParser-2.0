using DataBaseParser.MVVM.ViewModel;
using System.Windows;

namespace DataBaseParser
{
    public partial class MainWindowView : Window
    {
        private readonly MainWindowViewModel vm;
        private bool fileSave = false;

        public MainWindowView()
        {          
            InitializeComponent();
            vm = new();
            DataContext = vm;

            SearchButton.Click += (s, e) => vm.VulnerabilitySearch(SearchTextBox.Text, SelectedDataBaseComboBox.Text);

            ClearResaultButton.Click += (s, e) =>
            {
                vm.ResaultSearch.Clear();
                fileSave = false;
            };

            SaveResaultButton.Click += (s, e) =>
            {
                vm.SaveFile(vm.ResaultSearch);
                fileSave = true;
            };

            Closing += (s, e) =>
            {
                if (!fileSave && vm.ResaultSearch.Count > 0)
                {
                    if (MessageBox.Show("Вы желаете сохранить изменения?", "Информаця", MessageBoxButton.YesNo, MessageBoxImage.Information) is MessageBoxResult.Yes)
                        vm.SaveFile(vm.ResaultSearch);
                }
            };
        }
    }
}