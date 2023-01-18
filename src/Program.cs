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


        public static List<string> day_list;
        public static List<string> night_list;
        public static List<string> current_list = day_list;

        public static int wallpaper_changing_speed = 60;
        public static int wallpaper_swap_time = 17;
        public static string current_wallpaper_global = "";

        public static string logFile = workDir + "\\data\\log.txt";
        public static Thread dayCycle = new Thread(() => checkDayCycle(current_list, night_list, day_list));
        public static Thread changeWallpapers = new Thread(() => checkWallpaper(current_list, wallpaper_changing_speed * 1000));
        static void Main(string[] args)
        {
            Console.Title = "Wallpaper Changer";
            DateTime dt = DateTime.Now;
            if (!loadData())
            {
                string day_path = getPath("day");
                string night_path = getPath("night");

                saveData(day_path, night_path);

                day_list = getWalls(day_path);
                night_list = getWalls(night_path);
            }



            while (true)
            {
                
                Console.WriteLine("Wallpaper Changer - Menu");
                Console.WriteLine("____________________________________");
                Console.WriteLine("1. Change folder settings");
                Console.WriteLine("2. Change wallpaper changing speed");
                Console.WriteLine("3. Change wallpaper swap folder time");
                Console.WriteLine("4. Hide console");
                Console.WriteLine("5. Print log file");
                Console.WriteLine("6. Delete log file");
                Console.WriteLine("7. Print current settings");


                string input = Console.ReadLine();
                switch (input)
                {
                    case "1":
                        saveData(getPath("day"), getPath("night"));
                        loadData();
                        break;
                    case "2":
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
                    case "3":
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
                    case "4":
                        IntPtr hWnd = GetConsoleWindow();
                        if (hWnd != IntPtr.Zero)
                            ShowWindow(hWnd, 0);
                        break;
                    case "5":
                        try
                        {
                            string[] logs = File.ReadAllLines(logFile);
                            for (int i = 0; i < logs.Length; i++)
                            {
                                Console.WriteLine(logs[i]);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("log file is being used");
                        }
                        break;
                    case "6":
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
                    case "7":
                        try
                        {
                            Console.WriteLine();
                            string[] settings = File.ReadAllLines(workDir + "\\data\\settings.txt");
                            string[] dirs = File.ReadAllLines(workDir + "\\data\\wallpapers_dirs.txt");
                            if (settings.Length < 2 || dirs.Length < 2)
                                break;
                            Console.WriteLine($"Wallpaper changing speed: {settings[0]}s");
                            Console.WriteLine($"Wallpaper swap time: {settings[1]}h");
                            Console.WriteLine($"Day dir: {dirs[0]}");
                            Console.WriteLine($"Night dir: {dirs[1]}");
                            Console.WriteLine();
                        }
                        catch(Exception e)
                        {
                            log("Unknown error with printing settings");
                        }
                        break;
                    case "clear":
                        Console.Clear();
                        break;
                }
            }



        }

        public static string getPath(string which_dir)
        {
            Console.Write($"Enter full path of your {which_dir} wallpapers directory: ");
            return Console.ReadLine();
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
            checkAndCreateFile(workDir + "\\data\\wallpapers_dirs.txt");
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
    }
}
