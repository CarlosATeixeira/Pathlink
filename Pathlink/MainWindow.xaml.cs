using Pathlink.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
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
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Threading;

namespace Pathlink
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<Grid> grids = new List<Grid>();

        public MainWindow()
        {
            InitializeComponent();
            grids.Add(PathPage);
            grids.Add(TerrainPage);
            Terrain.SetPathInstance(Path);
            GeoData.SetPathInstance(Path);
        }

        private void PathCheck(object sender, RoutedEventArgs e)
        {
            foreach (Grid grid in grids)
            {
                if (grid == PathPage)
                {
                    grid.Visibility = Visibility.Visible;
                }
                else
                {
                    grid.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void TerrainCheck(object sender, RoutedEventArgs e)
        {
            foreach (Grid grid in grids)
            {
                if (grid == TerrainPage)
                {
                    grid.Visibility = Visibility.Visible;
                }
                else
                {
                    grid.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            
        }

        private void Min_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Max_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                this.WindowState = WindowState.Maximized;
            }
            else
            {
                this.WindowState = WindowState.Normal;
            }
        }

        private void Sair_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Terrain.SetPathInstance(Path);
            await GeoData.InitializeAsync();

        }

        private void Terrain_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private async void Path_Loaded(object sender, RoutedEventArgs e)
        {
            Path.LatA.Text = "25 54 12.46 S";
            Path.LongA.Text = "048 55 40.30 W";
            Path.LatB.Text = "25 57 39.82 S";
            Path.LongB.Text = "048 54 01.45 W";
            Path.Frequency.Text = "7.5";

            await GeoData.InitializeAsync();
            Path.UpdateAlts();
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

    }
}