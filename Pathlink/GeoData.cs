using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pathlink
{
    internal static class GeoData
    {
        static Path pathInstance;
        internal static double LatA_Decimal { get; set;}
        internal static double LongA_Decimal {get; set;}
        internal static double LatB_Decimal  {get; set;}
        internal static double LongB_Decimal {get; set;}

        internal static int ms = 5;

        internal static float frequency = 13;

        internal static Tuple<double, double> CoordinatesSiteA_Decimal => new Tuple<double, double>(LatA_Decimal, LongA_Decimal);
        internal static Tuple<double, double> CoordinatesSiteB_Decimal => new Tuple<double, double>(LatB_Decimal, LongB_Decimal);

        internal static double altA, altB;
        internal static double TotalDistance;

        public static void SetPathInstance(Path instance)
        {
            pathInstance = instance;
        }

        public static async Task InitializeAsync()
        {
            LatA_Decimal = double.Parse(GeoCalculationService.ConvertToDecimal(pathInstance.LatA.Text), CultureInfo.InvariantCulture);
            LongA_Decimal = double.Parse(GeoCalculationService.ConvertToDecimal(pathInstance.LongA.Text), CultureInfo.InvariantCulture);
            LatB_Decimal = double.Parse(GeoCalculationService.ConvertToDecimal(pathInstance.LatB.Text), CultureInfo.InvariantCulture);
            LongB_Decimal = double.Parse(GeoCalculationService.ConvertToDecimal(pathInstance.LongB.Text), CultureInfo.InvariantCulture);

            // Initialize elevations
            var elevationsA = await GeoCalculationService.FetchElevations(new List<Tuple<double, double>> { CoordinatesSiteA_Decimal });
            var elevationsB = await GeoCalculationService.FetchElevations(new List<Tuple<double, double>> { CoordinatesSiteB_Decimal });
            if (elevationsA.Count > 0) altA = elevationsA[0];
            if (elevationsB.Count > 0) altB = elevationsB[0];

            // Calculate total distance
            TotalDistance = GeoCalculationService.CalculateDistance( CoordinatesSiteA_Decimal, CoordinatesSiteB_Decimal);
        }
    }

}
