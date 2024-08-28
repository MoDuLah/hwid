using System;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace HWIDFetcher
{
    class Program
    {
        static void Main(string[] args)
        {
            // Retrieve HWIDs
            string cpuId = GetCpuId();
            string diskId = GetDiskId();
            string macAddress = GetMacAddress();

            // Concatenate HWIDs
            string hwid = $"{cpuId}-{diskId}-{macAddress}";

            // Generate a random salt
            string salt = GenerateSalt();

            // Hash the HWID with the salt
            string hashedHwid = HashWithSalt(hwid, salt);

            // Save to a .txt file
            SaveToFile("hwid.txt", hashedHwid);

            Console.WriteLine("HWID has been generated and saved to hwid.txt.");
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

        // Generate Salt
        static string GenerateSalt()
        {
            byte[] saltBytes = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
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

        // Save to File
        static void SaveToFile(string fileName, string content)
        {
            File.WriteAllText(fileName, content);
        }
    }
}
