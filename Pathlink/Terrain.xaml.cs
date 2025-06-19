using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Input.Manipulations;
using System.Windows.Media;
using System.Windows.Shapes;
using static Pathlink.GeoCalculationService;
using static Pathlink.Terrain;

namespace Pathlink
{
    /// <summary>
    /// Interação lógica para Terrain.xam
    /// </summary>
    public partial class Terrain : UserControl
    {
        static Path pathInstance;

        int pathGenerateIncrement = 10;

        int minAlt = 10;
        int maxAlt = 200;
        private double melhorH1 = 0;
        private double melhorH2 = 0;
        private double increment = 1;

        private const int HGT_SIZE = 1201; // Tamanho padrão dos dados do SRTM3
        private const double EarthRadius = 6371000;

        List<List<double>> resultadosMatriz = new List<List<double>>();

        public Terrain()
        {
            InitializeComponent();
            // Adicione este evento no construtor após InitializeComponent();
            ElevationProfileDataGrid.PreviewMouseLeftButtonDown += ElevationProfileDataGrid_PreviewMouseLeftButtonDown;
        }

        public void SetPathInstance(Path instance)
        {
            pathInstance = instance;
        }

        private async void GenerateTerrain_Click(object sender, RoutedEventArgs e)
        {
            if (pathInstance != null)
            {
                // Limpar listas para novo terreno
                GeoCalculationService.ClearElevData();

                List<Tuple<double, double>> pathcoordinates = GeneratePath(new Tuple<double, double>(GeoData.LatA_Decimal, GeoData.LongA_Decimal), new Tuple<double, double>(GeoData.LatB_Decimal, GeoData.LongB_Decimal), pathGenerateIncrement);

                await GeoCalculationService.FetchElevations(pathcoordinates);

                GeoCalculationService.SmoothElevDataElevations(windowSize: 5);

                DrawElevationProfile();

                GeoCalculationService.TestElevDataIntegrity();


                LoadDataGrid();
                ElevationProfileCanvas.SizeChanged += (s, e) => DrawElevationProfile();
            }
            else
            {
                Debug.WriteLine("Path instance not set in Terrain.");
            }
        }

        public static double LerElevacao(byte[] buffer, int offset)
        {
            // Lê a elevação do buffer
            short elevationShort = BitConverter.ToInt16(buffer, offset);

            // Verifica se é necessário inverter a ordem dos bytes (little-endian para big-endian)
            if (BitConverter.IsLittleEndian)
            {
                elevationShort = IPAddress.NetworkToHostOrder(elevationShort);
            }

            // Converte a elevação para double e retorna
            return Convert.ToDouble(elevationShort);
        }

        public static bool StringContainsLetter(string input)
        {
            // Percorre cada caractere da string
            foreach (char c in input)
            {
                // Verifica se o caractere é uma letra
                if (Char.IsLetter(c))
                {
                    return true; // Retorna verdadeiro se encontrar uma letra
                }
            }
            return false; // Retorna falso se não encontrar nenhuma letra
        }

        private static List<Tuple<double, double>> GeneratePath(Tuple<double, double> start, Tuple<double, double> end, double intervalMeters)
        {
            double distance = CalculateDistance(start, end);
            int steps = (int)(distance / (intervalMeters / 1000.0));

            double latStep = (end.Item1 - start.Item1) / steps;
            double lonStep = (end.Item2 - start.Item2) / steps;

            // Preallocate the array of Tuples
            var path = new Tuple<double, double>[steps + 1];

            // Perform the loop in parallel
            Parallel.For(0, steps + 1, i =>
            {
                double lat = start.Item1 + latStep * i;
                double lon = start.Item2 + lonStep * i;
                path[i] = new Tuple<double, double>(lat, lon);
            });

            // Convert the array to a list before returning
            return path.ToList();
        }


        private static double CalculateDistance(Tuple<double, double> start, Tuple<double, double> end)
        {
            // Esta função calcula a distância "plana" entre dois pontos.
            var R = 6371e3; // Raio da Terra em metros
            var phi1 = start.Item1 * Math.PI / 180; // Phi, Lambda em radianos
            var phi2 = end.Item1 * Math.PI / 180;
            var deltaPhi = (end.Item1 - start.Item1) * Math.PI / 180;
            var deltaLambda = (end.Item2 - start.Item2) * Math.PI / 180;

            // Usando a fórmula de Haversine
            var a = Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2) +
                    Math.Cos(phi1) * Math.Cos(phi2) *
                    Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            var distance = R * c; // Distância em metros

            return distance / 1000;
        }

        private void DrawElevationProfile(double maxAntena = 0)
        {
            if (!GeoCalculationService.elevData.Any()) return; // Se não houver dados, retorna imediatamente

            double canvasWidth = ElevationProfileCanvas.ActualWidth;
            double canvasHeight = ElevationProfileCanvas.ActualHeight;

            // Calcula a altura máxima considerando tanto as elevações do terreno quanto os obstáculos
            double maxElevation = GeoCalculationService.elevData.Max(x => x.Elevation);
            double maxObstacleHeight = GeoCalculationService.elevData
                .Where(x => x.StepObstruction != null)
                .Select(x => x.Elevation + x.StepObstruction.ObstructionHeight)
                .DefaultIfEmpty(0) // Retorna 0 se não houver elementos
                .Max();
            double maxAntenasHeight = Math.Max(melhorH1 + GeoData.altA, melhorH2 + GeoData.altB);
            double maxHeight = Math.Max(Math.Max(Math.Max(maxElevation, maxObstacleHeight), maxAntenasHeight), maxAntena);
            double minElevation = GeoCalculationService.elevData.Min(x => x.Elevation);

            // Ajusta a escala vertical para caber no Canvas, descontando margens
            double verticalMargin = 20; // Margem vertical para não desenhar exatamente na borda do Canvas
            double verticalScale = (canvasHeight - 2 * verticalMargin) / (maxHeight - minElevation);

            ElevationProfileCanvas.Children.Clear();

            // Desenha o perfil de elevação
            for (int i = 0; i < GeoCalculationService.elevData.Count - 1; i++)
            {
                var startPoint = new Point(
                    (GeoCalculationService.elevData[i].DistanceFromA / GeoCalculationService.TotalDistance) * canvasWidth,
                    canvasHeight - ((GeoCalculationService.elevData[i].Elevation - minElevation) * verticalScale + verticalMargin));

                var endPoint = new Point(
                    (GeoCalculationService.elevData[i + 1].DistanceFromA / GeoCalculationService.TotalDistance) * canvasWidth,
                    canvasHeight - ((GeoCalculationService.elevData[i + 1].Elevation - minElevation) * verticalScale + verticalMargin));

                Line line = new Line
                {
                    X1 = startPoint.X,
                    Y1 = startPoint.Y,
                    X2 = endPoint.X,
                    Y2 = endPoint.Y,
                    Stroke = Brushes.Orange,
                    StrokeThickness = 2
                };

                ElevationProfileCanvas.Children.Add(line);
            }

            // Desenha os obstáculos
            foreach (var step in GeoCalculationService.elevData.Where(x => x.StepObstruction != null))
            {
                double xPosition = (step.DistanceFromA / GeoCalculationService.TotalDistance) * canvasWidth;
                double yPosition = canvasHeight - ((step.Elevation + step.StepObstruction.ObstructionHeight - minElevation) * verticalScale + verticalMargin);

                Rectangle rect = new Rectangle
                {
                    Width = 5,
                    Height = (step.StepObstruction.ObstructionHeight * verticalScale),
                    Fill = step.StepObstruction.ObstructionType == "Vegetação" ? Brushes.Green : Brushes.Gray,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };

                Canvas.SetLeft(rect, xPosition - rect.Width / 2);
                Canvas.SetTop(rect, yPosition - rect.Height); // Subtrai a altura para ajustar a posição base
                ElevationProfileCanvas.Children.Add(rect);
            }
        }

        private void LoadDataGrid()
        {
            ElevationProfileDataGrid.ItemsSource = null;
            ElevationProfileDataGrid.ItemsSource = GeoCalculationService.elevData;
        }

        private void ElevationProfileDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ElevationProfileDataGrid.SelectedItem is GeoCalculationService.StepClass selectedStep)
            {
                double distanceFromA = selectedStep.DistanceFromA;

                DrawElevationLine(distanceFromA);
            }
        }

        private void DrawElevationLine(double distance)
        {
            // Limpa a linha anterior
            var existingLine = ElevationProfileCanvas.Children.OfType<Line>().FirstOrDefault(l => l.Tag?.ToString() == "selectionLine");
            if (existingLine != null)
            {
                ElevationProfileCanvas.Children.Remove(existingLine);
            }

            double canvasWidth = ElevationProfileCanvas.ActualWidth;
            // Assume que o último ponto na lista tem a maior distância do ponto A
            double maxDistance = GeoCalculationService.elevData.Any() ? GeoCalculationService.elevData.Last().DistanceFromA : 0;

            // Calcula a posição X da linha com base na distância proporcional
            double xPosition = (distance / maxDistance) * canvasWidth;

            // Cria uma nova linha para representar a seleção
            Line selectionLine = new Line
            {
                X1 = xPosition,
                X2 = xPosition,
                Y1 = 0,
                Y2 = ElevationProfileCanvas.ActualHeight,
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                Tag = "selectionLine" // Tag para identificar a linha
            };

            ElevationProfileCanvas.Children.Add(selectionLine);
        }

        private void ElevationProfileCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (GeoCalculationService.elevData.Count != 0)
            {
                // Calcula a distância com base na posição X do clique
                var pointClicked = e.GetPosition(ElevationProfileCanvas);
                double canvasWidth = ElevationProfileCanvas.ActualWidth;
                // Assume que o último ponto na lista tem a maior distância do ponto A
                double maxDistance = GeoCalculationService.elevData.Last().DistanceFromA;
                double distanceClicked = (pointClicked.X / canvasWidth) * maxDistance;

                // Encontra a entrada mais próxima baseado na distância clicada
                var closestEntry = GeoCalculationService.elevData.OrderBy(item => Math.Abs(item.DistanceFromA - distanceClicked)).First();

                // Para selecionar a entrada correspondente no DataGrid, você precisa ter certeza de que o DataGrid está de fato usando `elevData` como sua fonte de dados
                ElevationProfileDataGrid.SelectedItem = closestEntry;

                // Opcional: Rola o DataGrid para a entrada selecionada
                ElevationProfileDataGrid.ScrollIntoView(closestEntry);
            }
        }

        private void ElevationProfileDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column.Header.ToString() == "Elevação" && e.EditAction == DataGridEditAction.Commit)
            {
                var textBox = e.EditingElement as TextBox;
                double newValue;
                if (textBox != null && double.TryParse(textBox.Text, out newValue))
                {
                    // Assumindo que seu DataGrid agora está usando `elevData` como sua fonte de dados
                    var selectedItem = e.Row.Item as GeoCalculationService.StepClass;
                    if (selectedItem != null)
                    {
                        // Atualiza a elevação na lista
                        selectedItem.Elevation = newValue;

                        // Atualiza o gráfico se necessário
                        DrawElevationProfile();
                    }
                }
            }
        }

        private void ElevationProfileDataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Identificar a célula clicada
            var hit = VisualTreeHelper.HitTest((Visual)sender, e.GetPosition((IInputElement)sender));
            if (hit.VisualHit is FrameworkElement element && element.DataContext is GeoCalculationService.StepClass)
            {
                DataGridCell cell = FindParent<DataGridCell>(element);
                if (cell != null)
                {
                    int columnIndex = cell.Column.DisplayIndex;
                    // Verifica se a coluna clicada é a coluna de obstáculos
                    if (ElevationProfileDataGrid.Columns.Count > columnIndex && ElevationProfileDataGrid.Columns[columnIndex].Header.ToString() == "Obstáculo")
                    {
                        var stepData = (GeoCalculationService.StepClass)cell.DataContext;
                        ShowObstructionDialog(stepData);
                        e.Handled = true; // Impede a propagação do evento
                    }
                }
            }
        }

        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            // Obtém o pai do objeto fornecido
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            // Se o pai for nulo, chegamos ao topo da árvore visual sem encontrar o tipo desejado
            if (parentObject == null) return null;

            // Se o pai for do tipo desejado, retornamos o pai
            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                // Se não, procuramos recursivamente mais acima na árvore
                return FindParent<T>(parentObject);
            }
        }

        private void ShowObstructionDialog(GeoCalculationService.StepClass stepData)
        {
            //var dialog = new ObstructionDialog();
            //Inicializa o diálogo com informações existentes de obstrução, se houver
            //if (stepData.StepObstruction != null)
            //{
            //    dialog.SelectedObstructionType = stepData.StepObstruction.ObstructionType;
            //    dialog.ObstructionHeight = stepData.StepObstruction.ObstructionHeight;
            //    dialog.ObstructionGrowthMargin = stepData.StepObstruction.ObstructionGrowthMargin;
            //}

            //if (dialog.ShowDialog() == true)
            //{
            //    // Atualiza ou cria nova obstrução com os valores obtidos do diálogo
            //    if (stepData.StepObstruction == null)
            //    {
            //        stepData.StepObstruction = new StepClass.Obstruction();
            //    }

            //    stepData.StepObstruction.ObstructionType = dialog.SelectedObstructionType;
            //    stepData.StepObstruction.ObstructionHeight = dialog.ObstructionHeight;
            //    stepData.StepObstruction.ObstructionGrowthMargin = dialog.ObstructionGrowthMargin;

            //    ElevationProfileDataGrid.Items.Refresh();
            //    DrawElevationProfile();
            //}
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            var mousePosition = e.GetPosition(this); // Obtem a posição do mouse em relação à janela

            // Ajusta a posição do Popup. Você pode adicionar um pequeno deslocamento se necessário
            TerrainOverlay.HorizontalOffset = mousePosition.X; // +10 é o deslocamento para não ficar exatamente embaixo do cursor
            TerrainOverlay.VerticalOffset = mousePosition.Y + 10;
        }

        private void ElevationProfileDataGrid_MouseEnter(object sender, MouseEventArgs e)
        {
            TerrainOverlay.IsOpen = true;
        }

        private void ElevationProfileDataGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            TerrainOverlay.IsOpen = false;
        }

        public void AutoHeight_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(GeoCalculationService.elevData.Any());
            if (GeoCalculationService.elevData.Any())
            {
                EncontrarMelhoresAlturas();
            }
        }

        public static double CalcularRaioFresnel(double d, double D, double f)
        {
            // f em GHz, d e D em quilômetros
            double lambda = 0.3 / f; // Comprimento de onda em metros (c = 3 * 10^8 m/s)
            double r = Math.Sqrt((lambda * d * (D - d)) / D);
            return r;
        }

        public static double CalcularAlturaAntenaB(double d, double hpc, double d1, double h1, double hA, double hB, double f, double mc)
        {
            double d2 = d - d1;
            // Converte a frequência em GHz para MHz para a fórmula
            double freqMHz = f * 1000;
            // Fator K comum para a refratividade padrão da atmosfera
            double K = 4.0 / 3.0;

            // Distância do ponto crítico até o site B
            double g26 = d - d2;

            // Raio de Fresnel
            double g28 = Math.Round(550 * Math.Sqrt((d2 * g26) / (d * freqMHz)), 2);

            // Correção da curvatura da Terra
            double g30 = Math.Round(d2 * g26 / (K * 12.74), 2);

            // Calcula hc como a soma de g28 e g30
            double hc = (g28 + g30);

            // Aplica a fórmula para calcular a altura da antena B considerando hc
            double h2 = ((d * (hpc + hc + mc + GeoData.ms - hB)) - d2 * (hA + h1 - hB)) / d1;

            return h2;
        }

        public void EncontrarMelhoresAlturas()
        {
            // Validação de frequência
            if (GeoData.frequency <= 0)
            {
                Debug.WriteLine("Frequência deve ser maior que zero.");
                return;
            }

            double menorDiferencaAlturas = double.MaxValue;
            double melhorH1 = double.MaxValue;
            double melhorH2 = double.MaxValue;

            List<List<double>> alturas = new List<List<double>>();

            for (double hA = minAlt; hA <= GeoData.altA + maxAlt; hA += 1)
            {
                double hB = minAlt;

                foreach (var step in GeoCalculationService.elevData)
                {
                    double h2NoPonto = CalcularAlturaAntenaB(GeoData.TotalDistance, step.Elevation, step.DistanceFromA, hA, hA, hB, GeoData.frequency, step.StepObstruction != null ? step.StepObstruction.ObstructionGrowthMargin : 0);
                    hB = Math.Max(hB, h2NoPonto);
                }

                alturas.Add(new List<double>() { hA, hB });
            }

            foreach (var altura in alturas)
            {
                Debug.WriteLine(altura[0] + " | " + altura[1]);

                double somaAlturas = altura[0] + altura[1];

                if(somaAlturas < menorDiferencaAlturas)
                {
                    menorDiferencaAlturas = somaAlturas;
                    melhorH1 = altura[0];
                    melhorH2 = altura[1];
                }
            }

            // Atualiza a interface com os melhores valores encontrados
            Dispatcher.Invoke(() =>
            {
                AlturaAntena1.Text = $"{melhorH1:N2} m";
                AlturaAntena2.Text = $"{melhorH2:N2} m";
                Debug.WriteLine($"Melhor altura para a antena A: {melhorH1} m");
                Debug.WriteLine($"Melhor altura para a antena B: {melhorH2} m");
            });

            // Desenha a linha de visão com as melhores alturas encontradas
            DrawBestResultLine(melhorH1, melhorH2);
        }

        private void DrawBestResultLine(double melhorH1, double melhorH2)
        {
            DrawElevationProfile(Math.Max(melhorH1+GeoData.altA, melhorH2+GeoData.altB));

            // Limpa a linha anterior, se houver
            var existingLine = ElevationProfileCanvas.Children.OfType<Line>().FirstOrDefault(l => l.Tag?.ToString() == "bestResultLine");
            if (existingLine != null)
            {
                ElevationProfileCanvas.Children.Remove(existingLine);
            }

            // Dimensiona o canvas
            double canvasWidth = ElevationProfileCanvas.ActualWidth;
            double canvasHeight = ElevationProfileCanvas.ActualHeight;

            // Encontra a elevação mínima e máxima do terreno
            double minElevation = GeoCalculationService.elevData.Min(x => x.Elevation);
            double maxElevation = GeoCalculationService.elevData.Max(x => x.Elevation) + Math.Max(melhorH1, melhorH2);

            // Escala vertical para caber no canvas, com margens
            double verticalMargin = 20;
            double verticalScale = (canvasHeight - 2 * verticalMargin) / (maxElevation - minElevation);

            // Calcula as posições Y para as alturas das antenas, incluindo a elevação do terreno
            double startYPosition = canvasHeight - ((melhorH1 + GeoCalculationService.elevData.First().Elevation - minElevation) * verticalScale + verticalMargin);
            double endYPosition = canvasHeight - ((melhorH2 + GeoCalculationService.elevData.Last().Elevation - minElevation) * verticalScale + verticalMargin);

            // Verifica se as posições Y estão dentro dos limites do canvas
            startYPosition = Math.Max(verticalMargin, Math.Min(canvasHeight - verticalMargin, startYPosition));
            endYPosition = Math.Max(verticalMargin, Math.Min(canvasHeight - verticalMargin, endYPosition));

            // Cria a nova linha que representa o melhor resultado
            Line bestResultLine = new Line
            {
                X1 = 0, // Início da linha no canto esquerdo do canvas
                Y1 = startYPosition,
                X2 = canvasWidth, // Fim da linha no canto direito do canvas
                Y2 = endYPosition,
                Stroke = Brushes.Blue, // Cor da linha
                StrokeThickness = 2, // Espessura da linha
                Tag = "bestResultLine" // Tag para identificar a linha
            };

            // Adiciona a linha ao canvas
            ElevationProfileCanvas.Children.Add(bestResultLine);

            // Adicione marcadores para verificar as posições
            Ellipse startMarker = new Ellipse
            {
                Fill = Brushes.Red,
                Width = 5,
                Height = 5
            };
            Canvas.SetLeft(startMarker, 0); // Começa no canto esquerdo do canvas
            Canvas.SetTop(startMarker, startYPosition);

            Ellipse endMarker = new Ellipse
            {
                Fill = Brushes.Red,
                Width = 5,
                Height = 5
            };
            Canvas.SetLeft(endMarker, canvasWidth - 5); // Fim no canto direito do canvas
            Canvas.SetTop(endMarker, endYPosition);

            ElevationProfileCanvas.Children.Add(startMarker);
            ElevationProfileCanvas.Children.Add(endMarker);
        }

        private void ElevateAntenaA(object sender, MouseButtonEventArgs e)
        {
            CalculateHeights(double.Parse(AlturaAntena1.Text.Trim('m')) + 1, true);
        }

        private void LowerAntenaA(object sender, MouseButtonEventArgs e)
        {
            CalculateHeights(double.Parse(AlturaAntena1.Text.Trim('m')) - 1, true);
        }

        private void ElevateAntenaB(object sender, MouseButtonEventArgs e)
        {
            CalculateHeights(double.Parse(AlturaAntena2.Text.Trim('m')) + 1, false);
        }

        private void LowerAntenaB(object sender, MouseButtonEventArgs e)
        {
            CalculateHeights(double.Parse(AlturaAntena2.Text.Trim('m')) - 1, false);
        }

        public double CalculateHeights(double h1Atual, bool aToB)
        {
            List<StepClass> _elevData = elevData;

            if (!aToB)
            {
                _elevData.Reverse();
            }

            // Validação de frequência
            if (GeoData.frequency <= 0)
            {
                Debug.WriteLine("Frequência deve ser maior que zero.");
            }

            double melhorH2 = double.MaxValue;

            // Inicialize h2AtualMax para a altura atual da antena B
            double h2AtualMax = minAlt;

            foreach (var step in _elevData)
            {
                double h2NoPonto = CalcularAlturaAntenaB(GeoData.TotalDistance, step.Elevation, step.DistanceFromA, h1Atual, GeoData.altA, GeoData.altB, GeoData.frequency, step.StepObstruction.ObstructionGrowthMargin != null ? step.StepObstruction.ObstructionGrowthMargin : 0);
                h2AtualMax = Math.Max(h2AtualMax, h2NoPonto);
            }

            if (!aToB)
            {
                // Atualiza a interface com os melhores valores encontrados
                Dispatcher.Invoke(() =>
                {
                    AlturaAntena2.Text = $"{melhorH1:N2} m";
                    AlturaAntena1.Text = $"{melhorH2:N2} m";
                });

                // Desenha a linha de visão com as melhores alturas encontradas
                DrawBestResultLine(melhorH2, melhorH1);

                return h2AtualMax;
            }

            // Atualiza a interface com os melhores valores encontrados
            Dispatcher.Invoke(() =>
            {
                AlturaAntena1.Text = $"{melhorH1:N2} m";
                AlturaAntena2.Text = $"{melhorH2:N2} m";
            });

            // Desenha a linha de visão com as melhores alturas encontradas
            DrawBestResultLine(melhorH1, melhorH2);

            return h2AtualMax;
        }
    }
}