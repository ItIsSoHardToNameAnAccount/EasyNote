using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace EasyNote
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Hide taskbar anyway.
            this.ShowInTaskbar = false;

            this.MouseDown += (s, e) =>
            {
                if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                {
                    DragMove();
                }
            };

            var loadedModel = LoadTreeView(noteBookDataFile);
            RebuildTreeView(noteBook, loadedModel);
        }

        private void Window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            this.Background = new SolidColorBrush(Color.FromArgb(0x33, 0x00, 0x00, 0x00));
        }

        private void Window_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!IsMouseOver)
            {
                this.Background = new SolidColorBrush(Color.FromArgb(0x19, 0x00, 0x00, 0x00));
            }
        }

        private void NoteBook_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var treeViewItem = GetTreeViewItemUnderMouse(e.GetPosition(noteBook));
            showContextMenu(treeViewItem);
        }

        private TreeViewItem GetTreeViewItemUnderMouse(Point position)
        {
            HitTestResult hitTestResult = VisualTreeHelper.HitTest(noteBook, position);
            DependencyObject obj = hitTestResult.VisualHit;

            while (obj != null && !(obj is TreeViewItem))
            {
                obj = VisualTreeHelper.GetParent(obj);
            }

            return obj as TreeViewItem;
        }

        private void showContextMenu(TreeViewItem treeViewItem)
        {
            ContextMenu contextMenu = new ContextMenu();
            
            Binding binding = new Binding("Background")
            {
                Source = this,
                Mode = BindingMode.OneWay
            };
            contextMenu.SetBinding(Control.BackgroundProperty, binding);
            
            Binding foregroundBinding = new Binding("Foreground")
            {
                Source = this,
                Mode = BindingMode.OneWay
            };
            contextMenu.SetBinding(Control.ForegroundProperty, foregroundBinding);

            MenuItem addItem = new MenuItem { Header = "Add Note" };
            if (treeViewItem == null)
            {
                addItem.Click += (s, e) => AddNote();
            }
            else
            {
                treeViewItem.IsSelected = true;
                addItem.Click += (s, e) => AddDetail(treeViewItem);
            }
            contextMenu.Items.Add(addItem);

            if (treeViewItem != null)
            {
                MenuItem removeItem = new MenuItem { Header = "Remove Note" };
                removeItem.Click += (s, e) => RemoveNote(treeViewItem);
                contextMenu.Items.Add(removeItem);
            }

            MenuItem hideItem = new MenuItem { Header = "Hide" };
            hideItem.Click += (s, e) => this.Hide();
            contextMenu.Items.Add(hideItem);

            MenuItem exitItem = new MenuItem { Header = "Quit" };
            exitItem.Click += (s, e) => ExitNote();
            contextMenu.Items.Add(exitItem);

            contextMenu.IsOpen = true;
        }

        private void AddNote()
        {
            TreeViewItem newItem = CreateTreeViewItem();
            noteBook.Items.Add(newItem);
        }

        private void RemoveNote(TreeViewItem item)
        {
            noteBook.Items.Remove(item);
        }

        private void AddDetail(TreeViewItem parentItem)
        {
            TreeViewItem newChildItem = CreateTreeViewItem();
            parentItem.Items.Add(newChildItem);
            parentItem.IsExpanded = true;
        }

        private TreeViewItem CreateTreeViewItem()
        {
            TreeViewItem treeViewItem = new TreeViewItem();

            TextBox textBox = new TextBox
            {
                Width = 300,
                Margin = new Thickness(0, 0, 0, 0)
            };

            textBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    FinalizeTreeViewItemHeader(treeViewItem, textBox.Text);
                }
            };

            textBox.LostFocus += (s, e) =>
            {
                FinalizeTreeViewItemHeader(treeViewItem, textBox.Text);
            };

            treeViewItem.Header = textBox;
            textBox.Text = "New Item";
            Dispatcher.BeginInvoke(new Action(() =>
            {
                textBox.Focus();
                textBox.SelectAll();
            }), DispatcherPriority.Input);

            return treeViewItem;
        }

        private void FinalizeTreeViewItemHeader(TreeViewItem treeViewItem, string headerText)
        {
            StackPanel stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            CheckBox checkBox = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
            TextBlock textBlock = new TextBlock
            {
                Text = headerText,
                VerticalAlignment = VerticalAlignment.Center
            };

            stackPanel.Children.Add(checkBox);
            stackPanel.Children.Add(textBlock);

            treeViewItem.Header = stackPanel;
        }

        private void ExitNote()
        {
            var treeModel = ConvertTreeViewToModel(noteBook);
            SaveTreeView(noteBookDataFile, treeModel);
            EasyNoteIcon.Dispose();
            Application.Current.Shutdown();
        }

        private List<TreeNodeModel> ConvertTreeViewToModel(TreeView treeView)
        {
            var model = new List<TreeNodeModel>();
            foreach (TreeViewItem item in treeView.Items)
            {
                var itemModel = ConvertTreeViewItemToModel(item);
                if(itemModel.IsChecked == false)
                {
                    model.Add(itemModel);
                }
            }
            return model;
        }

        private TreeNodeModel ConvertTreeViewItemToModel(TreeViewItem item)
        {
            var model = new TreeNodeModel
            {
                Name = (item.Header as StackPanel)?.Children[1] is TextBlock textBlock ? textBlock.Text : item.Header.ToString(),
                IsChecked = (item.Header as StackPanel)?.Children[0] is CheckBox checkBox && checkBox.IsChecked == true
            };

            foreach (TreeViewItem childItem in item.Items)
            {
                model.Children.Add(ConvertTreeViewItemToModel(childItem));
            }

            return model;
        }

        private void SaveTreeView(string filePath, List<TreeNodeModel> treeModel)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(treeModel, options);
            File.WriteAllText(filePath, jsonString);
        }

        private List<TreeNodeModel> LoadTreeView(string filePath)
        {
            if (File.Exists(filePath))
            {
                string jsonString = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<List<TreeNodeModel>>(jsonString);
            }
            return new List<TreeNodeModel>();
        }

        private void RebuildTreeView(TreeView treeView, List<TreeNodeModel> treeModel)
        {
            treeView.Items.Clear();
            foreach (var model in treeModel)
            {
                treeView.Items.Add(RebuildTreeViewItem(model));
            }
        }

        private TreeViewItem RebuildTreeViewItem(TreeNodeModel model)
        {
            var treeViewItem = CreateEditableTreeViewItem(model.Name, model.IsChecked);
            foreach (var childModel in model.Children)
            {
                treeViewItem.Items.Add(RebuildTreeViewItem(childModel));
            }
            return treeViewItem;
        }

        private TreeViewItem CreateEditableTreeViewItem(string headerText, bool isChecked)
        {
            TreeViewItem treeViewItem = new TreeViewItem();
            
            StackPanel stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            CheckBox checkBox = new CheckBox { VerticalAlignment = VerticalAlignment.Center, IsChecked = isChecked };
            TextBlock textBlock = new TextBlock
            {
                Text = headerText,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            stackPanel.Children.Add(checkBox);
            stackPanel.Children.Add(textBlock);
            treeViewItem.Header = stackPanel;

            return treeViewItem;
        }

        private void Icon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
        {
            this.Show();
        }

        private string noteBookDataFile = "noteBookData.json";
    }
}