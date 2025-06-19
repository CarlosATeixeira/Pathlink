using Newtonsoft.Json;
using Pathlink.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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
using static Pathlink.Terrain;

namespace Pathlink
{
    /// <summary>
    /// Interaction logic for Path.xaml
    /// </summary>
    public partial class Path : UserControl
    {
        MainWindow mw;

        public TextBox caminho => Caminho;

        public Path()
        {
            InitializeComponent();
            mw = (MainWindow)Application.Current.MainWindow;
        }

        private void Caminho_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (Directory.Exists(Caminho.Text))
            {
                Caminho.Foreground = new SolidColorBrush(Colors.Green);
            }
            else
            {
                Caminho.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        private void CoordsChanged(object sender, TextChangedEventArgs e)
        { 
            try
            {
                DistanciaTotal.Text = GeoData.TotalDistance.ToString();
            }
            catch
            {
                Debug.WriteLine("Error in DistanciaTotal calculation");
            }
        }

        public static bool IsValidCoordinate(double input)
        {
            // Validate coordinates
            return input >= -180 && input <= 180; // Simplified example
        }

        internal void Generate_Click(object sender, RoutedEventArgs e)
        {
            // NE = Not Empty
            var NE_Caminho = caminho.Text.Length > 0;

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

            if (NE_All && Directory.Exists(caminho.Text))
            {
                Erro_Preencher.Visibility = Visibility.Hidden;

                if (File.Exists("work/path.kml"))
                {
                    File.Delete("work/path.kml");
                }
                else
                {
                    File.Copy("file/path.kml", "work/path.kml");
                }

                if (File.Exists("work/path.kml") && System.IO.Path.IsPathRooted(caminho.Text))
                {
                    string text = File.ReadAllText("work/path.kml");
                    text = text.Replace("#SITEA", SiteA.Text);
                    text = text.Replace("#LATA", "" + GeoData.LatA_Decimal);
                    text = text.Replace("#LONGA", "" + GeoData.LongA_Decimal);
                    text = text.Replace("#ALTA", AltA.Text);

                    text = text.Replace("#SITEB", SiteB.Text);
                    text = text.Replace("#LATB", "" + GeoData.LatB_Decimal);
                    text = text.Replace("#LONGB", "" + GeoData.LongB_Decimal);
                    text = text.Replace("#ALTB", AltB.Text);
                    File.WriteAllText("work/path.kml", text);

                    if (File.Exists($"{caminho.Text}/path.kml")) { File.Delete($"{caminho.Text}/path.kml"); }

                    File.Move("work/path.kml", $"{caminho.Text}/path.kml");

                    Completed.Visibility = Visibility.Visible;
                }
            }
            else
            {
                Erro_Preencher.Visibility = Visibility.Visible;
            }
        }

        public string ConvertToDecimal(string geo)
        {
            string[] parts = geo.Split(' ');
            double degrees = Convert.ToDouble(parts[0]);
            double minutes = Convert.ToDouble(parts[1]);
            double seconds = Convert.ToDouble(parts[2].Replace(".", ",")); // Considera a cultura local para interpretação de ponto flutuante

            double decimalDegrees = degrees + (minutes / 60) + (seconds / 3600);

            if (parts[3].ToUpper().Contains("S") || parts[3].ToUpper().Contains("W"))
            {
                decimalDegrees *= -1;
            }

            // Utiliza a cultura invariante para garantir o uso do ponto como separador decimal
            return decimalDegrees.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }


        private async void Declination_Calc()
        {
            Declination.Text = "...";
            string _lata;
            string _longa;
            if (LatA.Text.Any(Char.IsWhiteSpace))
            { _lata = ConvertToDecimal(LatA.Text); }
            else { _lata = LatA.Text; }
            if (LongA.Text.Any(Char.IsWhiteSpace))
            { _longa = ConvertToDecimal(LongA.Text); }
            else { _longa = LongA.Text; }
            double declinacaoMagnetica =
                await ObterDeclinacaoMagnetica(_lata, _longa);
            Declination.Text = (Math.Round(declinacaoMagnetica, 2)).ToString();
        }

        public async Task<double> ObterDeclinacaoMagnetica(string latitude, string longitude)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                // Defina o cabeçalho (header) "User-Agent"
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/92.0.4515.107 Safari/537.36");

                // Crie a URL da API do NOAA para obter a declinação magnética
                string baseUrl = "https://www.ngdc.noaa.gov/geomag-web/calculators/calculateDeclination";
                string apiKey = "zNEw7";
                string apiUrl = $"{baseUrl}?lat1={latitude}&lon1={longitude}&key={apiKey}&resultFormat=json";

                // Envie a solicitação HTTP GET para a API do NOAA
                HttpResponseMessage response = await httpClient.GetAsync(apiUrl);

                // Verifique se a resposta da API é bem-sucedida
                if (response.IsSuccessStatusCode)
                {
                    // Leia o conteúdo da resposta como uma string JSON
                    string jsonContent = await response.Content.ReadAsStringAsync();

                    // Desserialize o JSON para um objeto NOAAMagneticDeclinationResponse
                    NOAAMagneticDeclinationResponse declinationResponse = JsonConvert.DeserializeObject<NOAAMagneticDeclinationResponse>(jsonContent);

                    // Acesse a propriedade que contém a declinação magnética
                    DeclinationResult result = declinationResponse.Results.FirstOrDefault();
                    double declination = result?.Declination ?? 0.0;

                    return declination;
                }
                else
                {
                    // A resposta falhou, lide com o erro adequadamente
                    throw new Exception("Erro na solicitação à API do NOAA");
                }
            }
        }

        internal void UpdateAlts()
        {
            AltA.Text = GeoData.altA.ToString();
            AltB.Text = GeoData.altB.ToString();
        }
    }
}