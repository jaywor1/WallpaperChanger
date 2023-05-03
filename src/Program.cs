using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using System.Diagnostics;


namespace WallpaperChanger
{
    internal class Program
    {

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("User32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int cmdShow);

        [DllImport("user32.dll",CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        private static readonly int SPIF_UPDATEINIFILE = 0x01;
        private static readonly int SPIF_SENDWININICHANGE = 0x02;
        private static readonly int SPIF_SENDCHANGE = SPIF_SENDWININICHANGE | SPIF_UPDATEINIFILE;
        private static readonly int SPI_SETDESKWALLPAPER = 20;
        private static string workDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        // Colors
        public const ConsoleColor HIGHLIGHT_COLOR = ConsoleColor.White;
        public const ConsoleColor DEFAULT_COLOR = ConsoleColor.Gray;

        // Lists
        public static List<string> day_list;
        public static List<string> night_list;
        public static List<string> current_list = day_list;

        // Settings
        public static string g_day_dir = "";
        public static string g_night_dir = "";
        public static int wallpaper_changing_speed = 60;
        public static int wallpaper_swap_time = 17;


        public static string current_wallpaper_global = "";
        public static string logFile = workDir + "\\data\\log.txt";
        public static Thread dayCycle;
        public static Thread changeWallpapers;
        static void Main(string[] args)
        {
            Console.Title = "Wallpaper Changer";


            Console.WriteLine(@" _       __      ____                               ________                               
| |     / /___ _/ / /___  ____ _____  ___  _____   / ____/ /_  ____ _____  ____ ____  _____
| | /| / / __ `/ / / __ \/ __ `/ __ \/ _ \/ ___/  / /   / __ \/ __ `/ __ \/ __ `/ _ \/ ___/
| |/ |/ / /_/ / / / /_/ / /_/ / /_/ /  __/ /     / /___/ / / / /_/ / / / / /_/ /  __/ /    
|__/|__/\__,_/_/_/ .___/\__,_/ .___/\___/_/      \____/_/ /_/\__,_/_/ /_/\__, /\___/_/     
                /_/         /_/                                         /____/             
");
            Console.WriteLine("Press key to start...");
            Console.ReadKey();
            Console.Clear();

            DateTime dt = DateTime.Now;
            if (!loadData())
            {
                string day_path = getPath("day");
                string night_path = getPath("night");

                saveData(day_path, night_path);

                day_list = getWalls(day_path);
                night_list = getWalls(night_path);
                g_day_dir = day_path;
                g_night_dir = night_path;
            }

            // Creates threads
            dayCycle = new Thread(() => checkDayCycle(current_list, night_list, day_list));
            changeWallpapers = new Thread(() => checkWallpaper(current_list, wallpaper_changing_speed * 1000));

            while (true)
            {
                int checkKeyVal = -2;
                int highlighted = 0;

                while (true)
                {
                    Console.Clear();
                    string[] options = { "Change folder settings", "Change wallpaper changing speed", "Change wallpaper swap folder time", "Hide console", "Print log file", "Delete log file", "Print current settings" };
                    
                    Console.WriteLine("-------------- Menu --------------");
                    for (int i = 0; i < options.Length; i++)
                    {
                        if (i == highlighted)
                        {
                            Console.ForegroundColor = HIGHLIGHT_COLOR;
                            Console.WriteLine($"--> {options[i]}");
                            Console.ForegroundColor = DEFAULT_COLOR;
                        }
                        else
                        {
                            Console.WriteLine(options[i]);
                        }
                    }

                    checkKeyVal = checkKey(Console.ReadKey());

                    if(checkKeyVal == 0)
                    {
                        break;
                    }
                    else if(checkKeyVal == -2)
                    {
                        continue;
                    }
                    else
                    {
                        if(highlighted + checkKeyVal < 0 || highlighted + checkKeyVal >= options.Length)
                        {
                            continue;
                        }
                        highlighted += checkKeyVal;
                    }
                }

                Console.Clear();

                switch (highlighted)
                {
                    case 0:
                        saveData(getPath("day"), getPath("night"));
                        loadData();
                        break;
                    case 1:
                        string s = "test";
                        while(!int.TryParse(s,out wallpaper_changing_speed))
                        {
                            Console.Write("Wallpaper changing speed (in seconds): ");
                            s = Console.ReadLine();
                        }
                        saveSettings();
                        changeWallpapers.Abort();
                        dayCycle.Abort();
                        dayCycle = new Thread(() => checkDayCycle(current_list, night_list, day_list));
                        changeWallpapers = new Thread(() => checkWallpaper(current_list, wallpaper_changing_speed * 1000));
                        dayCycle.Start();
                        changeWallpapers.Start();
                        break;
                    case 2:
                        string temp = "test";
                        while (!int.TryParse(temp, out wallpaper_swap_time))
                        {
                            Console.Write("Wallpaper swap time (in hours): ");
                            temp = Console.ReadLine();
                        }
                        saveSettings();
                        dayCycle.Abort();
                        dayCycle = new Thread(() => checkDayCycle(current_list, night_list, day_list));
                        dayCycle.Start();
                        break;
                    case 3:
                        IntPtr hWnd = GetConsoleWindow();
                        if (hWnd != IntPtr.Zero)
                            ShowWindow(hWnd, 0);
                        break;
                    case 4:
                        try
                        {
                            string[] logs = File.ReadAllLines(logFile);
                            for (int i = 0; i < logs.Length; i++)
                            {
                                Console.WriteLine(logs[i]);
                            }
                            Console.WriteLine("\nPress ENTER to get back to menu...");
                            Console.ReadLine();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("log file is being used");
                        }
                        break;
                    case 5:
                        Console.WriteLine("Are you sure you want to delete log files? (yes/no)");
                        if(Console.ReadLine().ToLower() == "yes")
                        {
                            try
                            {
                                File.Create(logFile);
                                Console.WriteLine("Log file deleted");
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("log file is being used");
                            }
                        }
                        else
                            Console.WriteLine("Log file wasn't deleted");
                        break;
                    case 6:
                        try
                        {
                            string[] settings = File.ReadAllLines(workDir + "\\data\\settings.txt");
                            string[] dirs = File.ReadAllLines(workDir + "\\data\\wallpapers_dirs.txt");
                            if (settings.Length < 2 || dirs.Length < 2)
                                break;
                            Console.WriteLine($"Wallpaper changing speed: {settings[0]}s");
                            Console.WriteLine($"Wallpaper swap time: {settings[1]}h");
                            Console.WriteLine($"Day dir: {dirs[0]}");
                            Console.WriteLine($"Night dir: {dirs[1]}");

                            Console.WriteLine("\nPress ENTER to get back to menu...");
                            Console.ReadLine();

                        }
                        catch(Exception e)
                        {
                            log("Unknown error with printing settings");
                        }
                        break;
                }
            }



        }

        public static string getPath(string which_dir)
        {
            Console.WriteLine("Press ENTER for same dir as before");
            Console.Write($"Enter full path of your {which_dir} wallpapers directory: ");

            string input = Console.ReadLine();
            if (input == "")
                if (which_dir == "day")
                    return g_day_dir;
                else if (which_dir == "night")
                    return g_night_dir;
                else
                    return g_day_dir;
            else
                return input;
        }

        public static List<string> getWalls(string dir)
        {

            List<string> list = new List<string>();
            foreach (string file in Directory.GetFiles(dir))
            {
                list.Add(file);
            }
            
            return list;
        }

        public static void saveData(string day_dir, string night_dir)
        {
            if (!Directory.Exists(workDir + "\\data"))
            {
                Directory.CreateDirectory(workDir + "\\data");
                log($"[saveData]: created folder {workDir + "\\data"}");
            }
            else
            {
                log($"[getWalls]: folder {workDir + "\\data"} already exists");
            }
            checkAndCreateFile("\\data\\wallpapers_dirs.txt");
            using(StreamWriter sw = new StreamWriter(workDir + "\\data\\wallpapers_dirs.txt"))
            {
                sw.WriteLine(day_dir);
                sw.WriteLine(night_dir);
                sw.Close();
            }

        }

        public static void saveSettings()
        {
            if (!Directory.Exists(workDir + "\\data"))
            {
                Directory.CreateDirectory(workDir + "\\data");
                log($"[saveData]: created folder {workDir + "\\data"}");
            }
            else
            {
                log($"[getWalls]: folder {workDir + "\\data"} already exists");
            }
            checkAndCreateFile("settings.txt");
            using (StreamWriter sw = new StreamWriter(workDir + "\\data\\settings.txt"))
            {
                sw.WriteLine(wallpaper_changing_speed);
                sw.WriteLine(wallpaper_swap_time);
            }

        }


        public static void checkAndCreateFile(string file)
        {
            if (!File.Exists(workDir + file))
            {
                var fileObject = File.Create(workDir + file);
                fileObject.Close();
                log($"[saveData]: created file {workDir + file}");
            }
        }

        public static void log(string s)
        {
            try
            {
                if (!Directory.Exists(workDir + "\\data"))
                {
                    Directory.CreateDirectory(workDir + "\\data");
                }
                File.AppendAllText(logFile, s + '\n');
            }
            catch(Exception e)
            {
                Console.WriteLine("[log]: Can't write inside of logFile because it's opened by other process");
            }

        }

        public static bool loadData()
        {
            if(Directory.Exists(workDir + "\\data"))
            {
                if(File.Exists(workDir + "\\data\\wallpapers_dirs.txt") && File.Exists(workDir + "\\data\\settings.txt"))
                {
                    string[] settings = File.ReadAllLines(workDir + "\\data\\settings.txt");

                    if (settings.Length < 2)
                        return false;

                    wallpaper_changing_speed = int.Parse(settings[0]);
                    wallpaper_swap_time = int.Parse(settings[1]);

                    string[] dirs = File.ReadAllLines(workDir + "\\data\\wallpapers_dirs.txt");

                    if (dirs[0] == "" || dirs[1] == "")
                        return false;

                    g_day_dir = dirs[0];
                    g_night_dir = dirs[1];


                    if (dirs.Length < 2)
                        return false;

                    day_list = getWalls(dirs[0]);
                    night_list = getWalls(dirs[1]);

                    DateTime dt = DateTime.Now;

                    if ((8 >= dt.Hour) || (dt.Hour >= wallpaper_swap_time))
                    {
                        current_list = night_list;
                    }
                    else if ((8 < dt.Hour) && (dt.Hour < wallpaper_swap_time))
                    {
                        current_list = day_list;
                    }
                    dayCycle = new Thread(() => checkDayCycle(current_list, night_list, day_list));
                    dayCycle.Start();
                    return true;
                }
                else
                    log("[loadData]: wallpaper_dirs.txt or settings.txt does not exist");

            }
            else
                log("[loadData]: data folder does not exist");
            return false;
        }


        public static void checkDayCycle(List<string> current_list, List<string> night_list, List<string> day_list)
        {
            while (true)
            {
                DateTime dt = DateTime.Now;

                if ((8 >= dt.Hour) || (dt.Hour >= wallpaper_swap_time))
                {
                    current_list = night_list;
                }
                else if ((8 < dt.Hour) && (dt.Hour < wallpaper_swap_time))
                {
                    current_list = day_list;
                }

                log($"[checkDayCycle]: Sleeping for {(60-dt.Minute) * 60000}ms");

                changeWallpapers.Abort();
                changeWallpapers = new Thread(() => checkWallpaper(current_list, wallpaper_changing_speed * 1000));
                changeWallpapers.Start();


                Thread.Sleep((60-dt.Minute) * 60000);
            }
        }
        public static void checkWallpaper(List<string> current_list, int changeTime)
        {
            Random r = new Random();
            while (true)
            {
                setWallpaper(current_list,r,current_wallpaper_global);
                log($"[checkWalllpaper]: Sleeping for {changeTime}ms");
                Thread.Sleep(changeTime);
            }
        }

        public static void setWallpaper(List<string> current_list,Random r, string current_wallpaper)
        {
            string new_wallpaper = current_list[r.Next(0, current_list.Count - 1)];
            if (new_wallpaper == current_wallpaper)
                setWallpaper(current_list, r, current_wallpaper);
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, new_wallpaper, SPIF_SENDCHANGE);
            log($"[setWallpaper]: {new_wallpaper}");
            current_wallpaper_global = new_wallpaper;
        }

        public static int checkKey(ConsoleKeyInfo cki)
        {
            switch (cki.Key)
            {
                case ConsoleKey.DownArrow:
                case ConsoleKey.S:
                    return 1;
                case ConsoleKey.UpArrow:
                case ConsoleKey.W:
                    return -1;
                case ConsoleKey.Enter:
                case ConsoleKey.Spacebar:
                    return 0;
                default:
                    return -2;
            }
        }
    }
}
