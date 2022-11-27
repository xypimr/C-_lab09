using System.Text.RegularExpressions;
using task01;
using Newtonsoft.Json;

namespace task02
{
    class Program
    {
        public static string? ApiKey;
        
        public static async Task<WeatherOBJ> GetWeather(City city)
        {
            var url = $"https://api.openweathermap.org/data/2.5/weather";
            var parameters = $"?lat={city.Latitude}&lon={city.Longitude}&appid={ApiKey}";

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(url);

            HttpResponseMessage? response = new();

            Console.WriteLine("Fetching data...");
            try
            {
                response = await client.GetAsync(parameters);
            }
            catch (System.Net.Http.HttpRequestException)
            {
                Console.WriteLine($"{city.Name} : Connection troubles, try again");
            }

            WeatherOBJ result = new WeatherOBJ();
            if (response.IsSuccessStatusCode)
            {
                var textRes = await response.Content.ReadAsStringAsync();
                result = JsonConvert.DeserializeObject<task01.WeatherOBJ>(textRes);
            }

            // Если смогли достать хоть какие-то данные, считаем среднее. Иначе - возвращаем null.
            Console.WriteLine($"In {city.Name} now {result.Temp:F1} degrees : {result.Description}");
            return result;
        }
        private static City[] _arrayOfCities = Array.Empty<City>();
        
        public static void ConsoleRun()
        {
            var input = Console.ReadLine();
            var notExit = true;
            switch (input)
            {
                case ("quit"):
                {
                    notExit = false;
                    break;
                }
                case ("options"):
                {
                    foreach (City entry in _arrayOfCities)
                    {
                        Console.WriteLine($"> {entry.Name}");
                    }

                    break;
                }
                default:
                {
                    try
                    {
                        City newCity = _arrayOfCities.Where(data => data.Name == input).First();
                        GetWeather(newCity).GetAwaiter();
                    }
                    catch
                    {
                        Console.WriteLine($"No \"{input}\" city found");
                    }

                    break;
                }
            }

            if (notExit) ConsoleRun();
        }

        static void Main()
        {
            try
            {
                ApiKey = File.ReadAllText("../../../API_KEY.txt");
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("\n\nCreate API_KEY.txt file in project's directory\n\n");
            }

            // Считываем базу городов
            using (FileStream input = File.Open("../../../city.txt", FileMode.Open))
            {
                StreamReader reader = new(input);

                City entry = new();

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine()?.Replace("\t", " ").Split(" ");
                    entry.Name = line[0];
                    for (int i = 1; i < line.Length - 2; i++) entry.Name += $" {line[i]}";
                    entry.Latitude = Convert.ToDouble(line[line.Length - 2].Replace(",", ""));
                    entry.Longitude = Convert.ToDouble(line[line.Length - 1]);
                    _arrayOfCities = _arrayOfCities.Append(entry).ToArray();
                }

                reader.Close();
            }

            // Инструкция и запуск цикла выполнения
            Console.WriteLine("Usage: \tType in a city name to get weather data \n" +
                              "\tType \"options\" to see the city list \n" +
                              "\tType \"quit\" to, well, quit \n" +
                              "\tType some gibberish to do nothing \n");
            ConsoleRun();
        }
    }
}