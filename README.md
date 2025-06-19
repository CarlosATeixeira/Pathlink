# 🛰️ Pathlink — Terrain and Link Path Analysis Tool

Pathlink is a desktop application built with WPF and C# that analyzes terrain elevation data and calculates optimal telecommunication paths between two geographical sites. It utilizes SRTM elevation files and NOAA's magnetic declination API to assist in precision RF planning and link visibility.

## 🚀 Features

- 📍 Convert coordinates to decimal degrees
- 🧭 Fetch magnetic declination data from NOAA
- 🌄 Load and process SRTM elevation files (`.hgt`)
- 📈 Smooth and visualize terrain profiles
- 📡 Simulate and calculate optimal antenna heights
- 🌿 Add terrain obstructions (vegetation/construction)
- 📂 Export KML paths with elevation and obstruction data

## 🧩 Project Structure

- `MainWindow.xaml.cs` — Entry point for UI logic
- `Terrain.xaml.cs` — Elevation processing and graph rendering
- `Path.xaml.cs` — Coordinate input, elevation fetch, and KML generation
- `GeoCalculationService.cs` — Core geographic and elevation math
- `GeoData.cs` — Central storage for input coordinates and elevation data
- `FileNamingService.cs` — SRTM file name generation
- `ObstructionDialog.xaml.cs` — User input for terrain obstacles
- `NOAAMagneticDeclinationResponse.cs` — NOAA API data models
- `RelayCommand.cs`, `ObservableObject.cs` — MVVM infrastructure helpers

## 📦 External APIs

- [NOAA Geomagnetic Calculator](https://www.ngdc.noaa.gov/geomag-web/): Magnetic declination data per coordinate.

## 📊 Dependencies

- .NET (WPF Desktop App)
- Newtonsoft.Json (for NOAA API parsing)

## 🖥️ Usage

1. Input coordinates in degrees/minutes/seconds format
2. Click **Generate Terrain**
3. Adjust obstacles and elevation smoothing as needed
4. Review elevation profile and calculate best antenna heights
5. Export results as `.kml`

## 🛠️ Requirements

- .NET 6 SDK or later
- WPF-compatible environment (Visual Studio recommended)
- SRTM `.hgt` files stored in `terrain_data/` relative to the executable

## 📝 Notes

- Magnetic declination API requires valid `key`
- Elevation data is smoothed using a moving average window
- KML path is dynamically generated with accurate coordinates and altitudes

---

Developed by Carlos Ainz — Engineering and Terrain Modeling Toolkit.
