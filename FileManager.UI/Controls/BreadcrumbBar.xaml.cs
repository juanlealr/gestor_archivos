using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace FileManager.UI.Controls
{
    public class BreadcrumbItem
    {
        public string Path { get; set; } = "";
        public string DisplayName { get; set; } = "";
    }

    public partial class BreadcrumbBar : UserControl
    {
        public ObservableCollection<BreadcrumbItem> BreadcrumbItems 
        {
            get { return (ObservableCollection<BreadcrumbItem>)GetValue(BreadcrumbItemsProperty); }
            set { SetValue(BreadcrumbItemsProperty, value); }
        }

        public static readonly DependencyProperty BreadcrumbItemsProperty =
            DependencyProperty.Register("BreadcrumbItems", typeof(ObservableCollection<BreadcrumbItem>), typeof(BreadcrumbBar),
                new PropertyMetadata(new ObservableCollection<BreadcrumbItem>()));

        public BreadcrumbBar()
        {
            InitializeComponent();
        }

        public void UpdateBreadcrumb(string path)
        {
            var items = new ObservableCollection<BreadcrumbItem>();

            var parts = path.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            string currentPath = "";

            foreach (var part in parts)
            {
                currentPath += part + "\\";
                items.Add(new BreadcrumbItem 
                { 
                    Path = currentPath.TrimEnd('\\'),
                    DisplayName = part
                });
            }

            BreadcrumbItems = items;
        }
    }
}
