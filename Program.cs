using System;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using TextCopy;

namespace HWIDFetcher
{
    class Program
    {
        static bool hasBeenPasted = false;
        static Timer timer;

        static void Main(string[] args)
        {
            // Retrieve HWIDs
            string cpuId = GetCpuId();
            string diskId = GetDiskId();
            string macAddress = GetMacAddress();

            // Concatenate HWIDs
            string hwid = $"{cpuId}-{diskId}-{macAddress}";

            // Generate a deterministic salt based on HWID
            string salt = GenerateDeterministicSalt(hwid);

            // Hash the HWID with the deterministic salt
            string hashedHwid = HashWithSalt(hwid, salt);

            // Copy the hashed HWID to the clipboard
            ClipboardService.SetText(hashedHwid);

            Console.WriteLine("HWID has been copied to the clipboard. You have 5 seconds to paste it.");

            // Start the timer
            timer = new Timer(TerminateProgram, null, 5000, Timeout.Infinite);

            // Start monitoring the clipboard in a separate thread
            Thread clipboardThread = new Thread(() => MonitorClipboard(hashedHwid));
            clipboardThread.SetApartmentState(ApartmentState.STA); // Set thread to single-threaded apartment state
            clipboardThread.Start();
        }

        // Get CPU ID
        static string GetCpuId()
        {
            string cpuId = string.Empty;
            var query = new ManagementObjectSearcher("select ProcessorId from Win32_Processor");
            foreach (var item in query.Get())
            {
                cpuId = item["ProcessorId"].ToString();
                break;
            }
            return cpuId;
        }

        // Get Disk ID
        static string GetDiskId()
        {
            string diskId = string.Empty;
            var query = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_PhysicalMedia");
            foreach (var item in query.Get())
            {
                diskId = item["SerialNumber"].ToString();
                break;
            }
            return diskId;
        }

        // Get MAC Address
        static string GetMacAddress()
        {
            string macAddress = string.Empty;
            var query = new ManagementObjectSearcher("SELECT MACAddress FROM Win32_NetworkAdapter WHERE MACAddress IS NOT NULL AND Manufacturer != 'Microsoft'");
            foreach (var item in query.Get())
            {
                macAddress = item["MACAddress"].ToString();
                break;
            }
            return macAddress;
        }

        // Generate a deterministic salt based on the HWID
        static string GenerateDeterministicSalt(string hwid)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hwidBytes = Encoding.UTF8.GetBytes(hwid);
                byte[] saltBytes = sha256.ComputeHash(hwidBytes);
                return Convert.ToBase64String(saltBytes);
            }
        }

        // Hash with Salt
        static string HashWithSalt(string input, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input + salt);
                byte[] hashedBytes = sha256.ComputeHash(inputBytes);
                return Convert.ToBase64String(hashedBytes);
            }
        }

        // Monitor the clipboard and clear it after the first paste
        static void MonitorClipboard(string expectedText)
        {
            string lastClipboardText = ClipboardService.GetText();

            while (!hasBeenPasted)
            {
                string currentClipboardText = ClipboardService.GetText();

                if (currentClipboardText != lastClipboardText)
                {
                    hasBeenPasted = true;
                    ClearClipboard();
                    Console.WriteLine("Clipboard has been cleared after the first paste.");
                    TerminateProgram(null);  // Terminate immediately after pasting
                }

                Thread.Sleep(100); // Check every 100ms
            }
        }

        // Clear the clipboard and overwrite with dummy data
        static void ClearClipboard()
        {
            ClipboardService.SetText(string.Empty); // First clear
            Thread.Sleep(50); // Wait a bit
            ClipboardService.SetText("cleared"); // Overwrite with dummy data
            Thread.Sleep(50); // Wait a bit more
            ClipboardService.SetText(string.Empty); // Clear again
        }

        // Terminate the program after 20 seconds
        static void TerminateProgram(object state)
        {
            if (!hasBeenPasted)
            {
                ClearClipboard(); // Ensure clipboard is cleared
                Console.WriteLine("Time's up! The program will now terminate, and the clipboard has been cleared.");
            }
            Environment.Exit(0);
        }
    }
}
