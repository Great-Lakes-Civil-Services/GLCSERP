using System.Windows;
using System.Windows.Controls;
using CivilProcessERP.ViewModels;
using CivilProcessERP.Models.Job;

namespace CivilProcessERP.Views{
    public partial class JobDetailsView : UserControl// ✅ Change from UserControl to Window
    {
        public Job Job { get; set; }

        public JobDetailsView(Job job)
        {
            InitializeComponent();
            Job = job ?? new Job(); // ✅ Prevents null issue
            DataContext = this;
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
{
    if (this.Parent is Panel parentPanel)
    {
        parentPanel.Children.Remove(this);
    }
}
    }
}
