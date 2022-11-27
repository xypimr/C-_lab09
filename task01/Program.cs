using System.Net;
using System.Security;
using System.Text;

namespace task01;

static class Program
{

    private static int countTasks = 0;
    private static readonly HttpClient Client = new();

    static async Task<decimal?> AveragePrice(string tiker)
    {
        var dateToday = DateTimeOffset.Now.ToUnixTimeSeconds();
        var dateYearAgo = DateTimeOffset.Now.AddYears(-1).ToUnixTimeSeconds();
        var queryPattern =
            $"https://query1.finance.yahoo.com/v7/finance/download/{tiker}?" +
            $"period1={dateYearAgo}&period2={dateToday}&interval=1d&events=history&includeAdjustedClose=true";
        var response = await Client.GetAsync(queryPattern);
        var textRes = await response.Content.ReadAsStringAsync();
        List<string> days = new List<string>(textRes.Split('\n'));
        days.RemoveAt(0);
        int countOfDays = 0;
        decimal sum = 0;
        foreach (var dayInfo in days)
        {
            var listDayInfo = dayInfo.Split(',');
            if (listDayInfo.Length < 4)
            {
                continue;
            }

            if (listDayInfo[3] != "null" && listDayInfo[4] != "null")
            {
                sum += (decimal.Parse(listDayInfo[3]) + decimal.Parse(listDayInfo[4])) / 2;
                countOfDays++;
            }
        }

        if (countOfDays == 0)
        {
            return null;
        }
        else
        {
            return sum / countOfDays;
        }

    }

    static Mutex mutex = new();

    private static async Task AveragePriceToFile(StreamWriter outFile, string tiker)
    {
        var price = await AveragePrice(tiker);
        mutex.WaitOne();
        if (price != null)
        {
            outFile.WriteLine($"{tiker} : {price:C2}");
        }
        else
        {
            outFile.WriteLine($"{tiker} : can not get info!");
        }
        outFile.Flush();
        mutex.ReleaseMutex();
        countTasks--;
    }

    static void statusbar(int i)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.SetCursorPosition(0,2);
        string message = "Обработка данных: [";
        Console.WriteLine(message);
        Console.SetCursorPosition(message.Length + i*40/100, 2);
        Console.WriteLine("X");
        Console.SetCursorPosition(message.Length + 41, 2);
        Console.WriteLine($"] {i}%");
    }
    private static async Task RunAsync()
    {
        using (FileStream input = File.Open("../../../ticker.txt", FileMode.Open),
               output = File.Open("../../../avg.txt", FileMode.Create))
        {
            StreamReader inputReader = new StreamReader(input);
            StreamWriter outputWriter = new StreamWriter(output);
            int i = 0;
            while (!inputReader.EndOfStream)
            {
                statusbar((i*100/500)+1);
                i++;
                var tiker = inputReader.ReadLineAsync().GetAwaiter().GetResult();
                AveragePriceToFile(outputWriter, tiker);
                countTasks++;
            }
            while (countTasks != 0) {}
            
            Console.WriteLine("\nГотово!");
        }
    }

    static void Main()
        {
            RunAsync().GetAwaiter().GetResult();
        }
        
}
