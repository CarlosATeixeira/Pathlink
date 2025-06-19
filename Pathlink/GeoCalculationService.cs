using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Diagnostics;
using System.Runtime.InteropServices;
using static Pathlink.Terrain;
using System.IO;
using System.Windows.Threading;

namespace Pathlink
{
    public static class GeoCalculationService
    {
        internal static List<StepClass> elevData = new List<StepClass>();
        static int elevDataCount = 0;

        internal static double TotalDistance = CalculateDistance(new Tuple<double, double>(GeoData.LatA_Decimal, GeoData.LongA_Decimal), new Tuple<double, double>(GeoData.LatB_Decimal, GeoData.LongB_Decimal));

        internal class StepClass
        {
            public int Count;
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public double DistanceFromA { get; set; }
            public double DistanceFromB { get; set; }
            public double Elevation { get; set; }

            public Obstruction? StepObstruction { get; set; } // Pode ser null se não houver obstáculo

            public class Obstruction
            {
                public string ObstructionType { get; set; } = string.Empty;
                public double ObstructionHeight { get; set; }
                public int ObstructionGrowthMargin { get; set; }
                public string ObstructionDisplay => $"{(ObstructionType == "Vegetação" ? "Veg." : "Con.")} - {ObstructionHeight}m + {ObstructionGrowthMargin}m";
            }
        }

        public static void ClearElevData()
        {
            elevData.Clear();
        }

        public static string ConvertToDecimal(string geo)
        {
            // Ensure parts are split correctly, assuming space-separated inputs.
            string[] parts = geo.Split(' ');
            if (parts.Length < 4)
            {
                throw new ArgumentException("Input string is not in the correct format.");
            }

            // Parse degrees, minutes, and seconds using the invariant culture.
            double degrees = double.Parse(parts[0], CultureInfo.InvariantCulture);
            double minutes = double.Parse(parts[1], CultureInfo.InvariantCulture);
            double seconds = double.Parse(parts[2], CultureInfo.InvariantCulture);

            // Calculate the decimal degrees.
            double decimalDegrees = degrees + (minutes / 60) + (seconds / 3600);

            // Adjust for south and west coordinates to be negative.
            if (parts[3].ToUpper().Contains("S") || parts[3].ToUpper().Contains("W"))
            {
                decimalDegrees *= -1;
            }

            // Convert to string using invariant culture to maintain the decimal separator as '.'
            return decimalDegrees.ToString(CultureInfo.InvariantCulture);
        }

        public static double CalculateDistance(Tuple<double, double> start, Tuple<double, double> end)
        {
            // This is a simplified version of the Haversine formula for calculating distance between two points on the Earth's surface.
            var R = 6371e3; // Radius of Earth in meters
            var phi1 = start.Item1 * Math.PI / 180;
            var phi2 = end.Item1 * Math.PI / 180;
            var deltaPhi = (end.Item1 - start.Item1) * Math.PI / 180;
            var deltaLambda = (end.Item2 - start.Item2) * Math.PI / 180;

            var a = Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2) +
                    Math.Cos(phi1) * Math.Cos(phi2) *
                    Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            var distance = R * c; // Distance in meters
            return distance / 1000; // Convert to kilometers
        }

        public static int GetDegrees(double decimalCoord, bool isLatitude)
        {
            // Convert the decimal degree to an absolute value
            decimalCoord = Math.Abs(decimalCoord);

            // Calculate the degrees
            int degrees = (int)decimalCoord;

            return degrees;
        }

        public static async Task<List<double>> FetchElevations(List<Tuple<double, double>> coordinates)
        {
            // Primeiro, determine os arquivos SRTM únicos necessários para todas as coordenadas
            var uniqueFiles = coordinates.Select(coordinate =>
                FileNamingService.DetermineSrtmFileName(coordinate.Item1, coordinate.Item2)).Distinct();


            // Crie um dicionário para armazenar o conteúdo dos arquivos
            var fileContents = new Dictionary<string, byte[]>();

            foreach (var fileName in uniqueFiles)
            {
                string srtmFilePath = FileNamingService.GetSrtmFilePath(fileName);
                byte[] fileData = await File.ReadAllBytesAsync(srtmFilePath);
                fileContents[fileName] = fileData;
            }

            // Agora, leia as elevações do buffer em vez do disco
            var elevations = coordinates.Select(coordinate =>
            {
                string srtmFileName = FileNamingService.DetermineSrtmFileName(coordinate.Item1, coordinate.Item2);
                // Converte explicitamente short para double
                return (double)ReadElevationFromBuffer(fileContents[srtmFileName], coordinate.Item1, coordinate.Item2);
            }).ToList();

            return elevations;
        }

        private static double ReadElevationFromBuffer(byte[] buffer, double latitude, double longitude)
        {
            const int HGT_SIZE = 1201; // Tamanho padrão dos dados do SRTM3

            // Calcula a posição de linha e coluna dentro do buffer
            double latFraction = latitude - Math.Floor(latitude);
            double lonFraction = longitude - Math.Floor(longitude);
            int row = (int)((1.0 - latFraction) * (HGT_SIZE - 1));
            int col = (int)(lonFraction * (HGT_SIZE - 1));

            // Calcula o deslocamento no buffer
            int offset = (row * HGT_SIZE + col) * sizeof(short);
            if (offset < buffer.Length - sizeof(short))
            {
                // Lê a elevação do buffer
                double elevation = LerElevacao(buffer, offset);

                double distanceFromA = CalculateDistance(GeoData.CoordinatesSiteA_Decimal, new Tuple<double, double>(latitude, longitude));
                double distanceFromB = GeoData.TotalDistance - distanceFromA;

                // Adiciona o novo StepClass ao elevData com as propriedades preenchidas
                elevDataCount++;
                elevData.Add(new StepClass
                {
                    Count = elevDataCount,
                    Latitude = latitude,
                    Longitude = longitude,
                    DistanceFromA = distanceFromA,
                    DistanceFromB = distanceFromB,
                    Elevation = elevation
                });

                return elevation;
            }
            else
            {
                throw new ArgumentOutOfRangeException("A posição calculada está fora dos limites do buffer.");
            }
        }

        internal static void TestElevDataIntegrity()
        {
            Parallel.ForEach(elevData.Select(x => x.Elevation), (elevation, state) =>
            {
                if (double.IsNaN(elevation) || double.IsInfinity(elevation))
                {
                    Debug.WriteLine("Elevação inválida encontrada.");
                    state.Stop(); // Stop all parallel operations
                }
            });
        }

        internal static void SmoothElevDataElevations(int windowSize)
        {
            // Extrai as elevações da lista elevData
            List<double> elevations = elevData.Select(step => step.Elevation).ToList();

            // Aplica o suavizador às elevações
            List<double> smoothedElevations = SmoothElevations(elevations, windowSize);

            // Atualiza elevData com as elevações suavizadas
            for (int i = 0; i < elevData.Count; i++)
            {
                elevData[i].Elevation = smoothedElevations[i];
            }
        }

        static List<double> SmoothElevations(List<double> elevations, int windowSize)
        {
            List<double> smoothedElevations = new List<double>();
            for (int i = 0; i < elevations.Count; i++)
            {
                double sum = 0;
                int count = 0;
                for (int j = Math.Max(0, i - windowSize); j <= Math.Min(elevations.Count - 1, i + windowSize); j++)
                {
                    sum += elevations[j];
                    count++;
                }
                smoothedElevations.Add(sum / count);
            }
            return smoothedElevations;
        }

        
    }

}
