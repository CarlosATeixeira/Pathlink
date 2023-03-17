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

namespace Pathlink
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            
        }

        private void Sair_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void Caminho_TextChanged(object sender, TextChangedEventArgs e)
        {
            Trace.WriteLine("changed");

            if (Directory.Exists(Caminho.Text))
            {
                Caminho.Foreground = new SolidColorBrush(Colors.Green);
            }
            else
            {
                Caminho.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        private void Generate_Click(object sender, RoutedEventArgs e)
        {
            // NE = Not Empty
            var NE_Caminho = Caminho.Text.Length > 0;

            var NE_SiteA = SiteA.Text.Length > 0;
            var NE_LatA = LatA.Text.Length > 0;
            var NE_LongA = LongA.Text.Length > 0;
            var NE_AltA = AltA.Text.Length > 0;

            var NE_SiteB = SiteB.Text.Length > 0;
            var NE_LatB = LatB.Text.Length > 0;
            var NE_LongB = LongB.Text.Length > 0;
            var NE_AltB = AltB.Text.Length > 0;

            // Verifica se todos campos estão preenchidos
            var NE_All = NE_Caminho && NE_SiteA && NE_LatA && NE_LongA && NE_AltA && NE_SiteB && NE_LatB && NE_LongB && NE_AltB;

            if (NE_All && Directory.Exists(Caminho.Text))
            {
                if (File.Exists("work/path.kml"))
                {
                    File.Delete("work/path.kml");
                }
                else
                {
                    File.Copy("file/path.kml", "work/path.kml");
                }

                if (File.Exists("work/path.kml") && System.IO.Path.IsPathRooted(Caminho.Text))
                {
                    if (LatA.Text.Any(Char.IsWhiteSpace)) { LatA.Text = ConvertToDecimal(LatA.Text); }
                    if (LongA.Text.Any(Char.IsWhiteSpace)) { LongA.Text = ConvertToDecimal(LongA.Text); }

                    if (LatB.Text.Any(Char.IsWhiteSpace)) { LatB.Text = ConvertToDecimal(LatB.Text); }
                    if (LongB.Text.Any(Char.IsWhiteSpace)) { LongB.Text = ConvertToDecimal(LongB.Text); }

                    string text = File.ReadAllText("work/path.kml");
                    text = text.Replace("#SITEA", SiteA.Text);
                    text = text.Replace("#LATA", LatA.Text);
                    text = text.Replace("#LONGA", LongA.Text);
                    text = text.Replace("#ALTA", AltA.Text);

                    text = text.Replace("#SITEB", SiteB.Text);
                    text = text.Replace("#LATB", LatB.Text);
                    text = text.Replace("#LONGB", LongB.Text);
                    text = text.Replace("#ALTB", AltB.Text);
                    File.WriteAllText("work/path.kml", text);

                    if (File.Exists($"{Caminho.Text}/path.kml")) { File.Delete($"{Caminho.Text}/path.kml"); }

                    File.Move("work/path.kml", $"{Caminho.Text}/path.kml");

                    Completed.Visibility = Visibility.Visible;
                }
            }
            else
            {
                Erro_Preencher.Visibility = Visibility.Visible;
            }
        }

        private string ConvertToDecimal(string geo)
        {
            double dec;
            string decString;

            string[] geoPart = geo.Split(' ');
            string[] geoPart2 = geoPart[2].Split(".");

            var a = Convert.ToDouble(geoPart[0]);
            var b = Convert.ToDouble(geoPart[1]);
            var c = Convert.ToDouble(geoPart2[0]) + (Convert.ToDouble(geoPart2[1])*0.01);


            dec = a + (b / 60) + (c / 3600);

            if (geoPart[3].Contains("S") || geoPart[3].Contains("W"))
            {
                dec *= -1;
            }

            foreach (string part in geoPart) { Trace.WriteLine(part); }
            Trace.WriteLine(a);
            Trace.WriteLine(b);
            Trace.WriteLine(c);
            Trace.WriteLine(dec);

            decString = dec.ToString().Replace(",",".");

            return decString;
        }
    }
}
