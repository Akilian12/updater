using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace updaterTest
{
    class Program
    {
        private static int lastDownloadPercent = -1;
        private static Uri downloadUri = new Uri(@"https://s3.eu-central-1.amazonaws.com/blob-eu-central-1-nwsekq/sara/62/62dd/62ddbff8-4a94-47b4-be74-a3c673bbab51.bin?response-content-disposition=attachment%3B%20filename%3D%22updaterTest.exe%22&response-content-type=application%2Fx-msdownload&X-Amz-Content-Sha256=UNSIGNED-PAYLOAD&X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=AKIAI75SICYCOZ7DPWTA%2F20210415%2Feu-central-1%2Fs3%2Faws4_request&X-Amz-Date=20210415T154937Z&X-Amz-SignedHeaders=host&X-Amz-Expires=1800&X-Amz-Signature=6e1c5136dfbe0964195aa6d027e23c75c47af88b0ff253972eb58af51cd9ade5");
        static async Task Main(string[] args)
        {
            if (File.Exists($"{System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName}.old"))
            {
                File.Delete($"{System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName}.old");
                Console.WriteLine("Remove old version file");
            }
            if (NeedUpdate())
            {
                Console.WriteLine("Start downloading update");
                bool result = await DownloadUpdate();
                if (!result)
                    throw new Exception("Error during update");
                File.Move(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName, $"{System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName}.old");
                File.Move($"{System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName}.new", System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                System.Diagnostics.Process.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                Console.WriteLine("Update completed");
                Environment.Exit(0);
            }
        }

        private static bool NeedUpdate()
        {
            Console.WriteLine($"Current version is {Assembly.GetExecutingAssembly().GetName().Version.ToString()}");
            Version serverVersion = GetVersionFromServer();
            Console.WriteLine($"Server version is {serverVersion.ToString()}");
            return serverVersion > Assembly.GetExecutingAssembly().GetName().Version;
        }

        private static Version GetVersionFromServer()
        {
            return new Version(1, 0, 1, 2);
        }
        
        private static String GetMD5Summ()
        {
            return "a270e681b641af249510043ed73ee167";
        }

        private static async Task<bool> DownloadUpdate()
        {
            using (WebClient client = new WebClient())
            {
                try
                {
                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressCallback);
                    client.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadFileCallback);
                    Console.WriteLine();
                    await client.DownloadFileTaskAsync(downloadUri, $"{System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName}.new");
                    return Checksumm($"{System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName}.new", GetMD5Summ());
                }
                catch(Exception e)
                {
                    File.Delete($"{System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName}.new");
                    Console.WriteLine(e.ToString());
                    return false;
                }
            }
        }

        private static void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage % 10 == 0 && e.ProgressPercentage > lastDownloadPercent)
            {
                lastDownloadPercent = e.ProgressPercentage;
                Console.WriteLine("{0} % complete...", e.ProgressPercentage);
            }
        }

        private static void DownloadFileCallback(object sender, AsyncCompletedEventArgs e)
        {
            lastDownloadPercent = -1;
            if (e.Cancelled)
                throw new Exception("File download cancelled");

            if (e.Error != null)
                throw e.Error;

            if (!e.Cancelled && e.Error == null)
                Console.WriteLine("File downloaded");               
        }

        private static bool Checksumm(string filename, string summ)
        {
            using (FileStream fs = File.OpenRead(filename))
            {
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] fileData = new byte[fs.Length];
                fs.Read(fileData, 0, (int)fs.Length);
                byte[] checkSumm = md5.ComputeHash(fileData);
                if (BitConverter.ToString(checkSumm).Replace("-", String.Empty) != (summ).ToUpper())
                    File.Delete(filename);
                return BitConverter.ToString(checkSumm).Replace("-", String.Empty) == (summ).ToUpper();
            }
        }
    }
}