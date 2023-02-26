using System;

namespace Tools
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                try
                {
                    ConfigYml.AccessKeyId = args[0];
                    ConfigYml.AccessKeySecret = args[1];
                    ConfigYml.Endpoint = args[2];
                    ConfigYml.BucketName = args[3];
                    ConfigYml.ProjectName = args[4];
                    ConfigYml.FileToPut = args[5];
                    ConfigYml.FilePath = args[6];
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            try
            {
                OSSHelper oss = new OSSHelper();
                oss.Put(ConfigYml.FilePath, ConfigYml.ProjectName, ConfigYml.BucketName);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed with error info: {0}", ex.Message);
            }

            // Console.WriteLine("Press any key to continue . . . ");
            // Console.ReadKey(true);
        }
    }
}