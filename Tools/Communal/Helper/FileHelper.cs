using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Tools
{
    /// <summary>
    /// 文件公共处理类
    /// </summary>
    public static class FileHelper
    {
        
        public static void Save(string file, string filePath)
        {
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                using (StreamWriter stream = new StreamWriter(fileStream, Encoding.UTF8))
                {
                    stream.Write(file);
                }
            }

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"{filePath} 存储完毕");
        }
        
        public static string Load(string filePath)
        {
            string ret = string.Empty;
            if (!File.Exists(filePath))
            {
                return string.Empty;
            }

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
            {
                using (StreamReader stream = new StreamReader(fileStream, Encoding.UTF8))
                {
                    ret = stream.ReadToEnd();
                }
            }
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine($"{filePath} 读取完毕");
            return ret;
        }
        
        /// <summary>
        /// 将文件转换成byte[]数组
        /// </summary>
        /// <param name="fileUrl">文件路径文件名称</param>
        /// <returns>byte[]数组</returns>
        public static byte[] FileToByte(string fileUrl)
        {
            try
            {
                using (FileStream fs = new FileStream(fileUrl, FileMode.Open, FileAccess.Read))
                {
                    byte[] byteArray = new byte[fs.Length];
                    fs.Read(byteArray, 0, byteArray.Length);
                    return byteArray;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 将byte[]数组保存成文件
        /// </summary>
        /// <param name="byteArray">byte[]数组</param>
        /// <param name="fileName">保存至硬盘的文件路径</param>
        /// <returns></returns>
        public static bool ByteToFile(byte[] byteArray, string fileName)
        {
            bool result = false;
            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    fs.Write(byteArray, 0, byteArray.Length);
                    result = true;
                }
            }
            catch
            {
                result = false;
            }
            return result;
        }
        
        public static string GetHash(string path)
        {
            //var hash = SHA256.Create();
            //var hash = MD5.Create();
            var hash = SHA1.Create();
            var stream = new FileStream(path, FileMode.Open);
            byte[] hashByte = hash.ComputeHash(stream);
            stream.Close();
            return BitConverter.ToString(hashByte).Replace("-", "");
        }
        
    }

}
