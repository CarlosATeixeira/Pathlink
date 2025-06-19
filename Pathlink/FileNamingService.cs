using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pathlink
{
    public class FileNamingService
    {
        public static string DetermineSrtmFileName(double latitude, double longitude)
        {
            // Determine hemisphere prefixes
            char latPrefix = latitude >= 0 ? 'N' : 'S';
            char lonPrefix = longitude >= 0 ? 'E' : 'W';

            Debug.WriteLine($"{latPrefix}{GeoCalculationService.GetDegrees(latitude, true)+1}{lonPrefix}0{GeoCalculationService.GetDegrees(longitude, false)+1}.hgt");

            return $"{latPrefix}{GeoCalculationService.GetDegrees(latitude, true)+1}{lonPrefix}0{GeoCalculationService.GetDegrees(longitude, false)+1}.hgt";
        }

        // Método modificado para construir o caminho do arquivo SRTM baseado no local do executável.
        public static string GetSrtmFilePath(string srtmFileName)
        {
            var exeLocation = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            // Certifique-se de que "terrain_data" apareça apenas uma vez no caminho.
            return System.IO.Path.Combine(exeLocation, "terrain_data", srtmFileName);
        }
    }
}
