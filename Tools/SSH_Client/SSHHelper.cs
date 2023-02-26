using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Zip;
using Renci.SshNet;

namespace Tools;

public class SSHHelper
{
    #region SFTP

    /// <summary>
    /// 指定文件目录上传
    /// </summary>
    /// <param name="airPath"></param>
    /// <param name="tarPath"></param>
    public void SFTP(string airPath, string tarPath, bool zip)
    {

        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        PrivateKeyFile keyFiles = new PrivateKeyFile(ConfigYml.keyFiles);

        var sftp = new SftpClient(ConfigYml.SSH_IP, ConfigYml.SSH_Port, "root", keyFiles); //创建ssh连接对象
        sftp.Connect(); //  连接

        var ssh = new SshClient(ConfigYml.SSH_IP, ConfigYml.SSH_Port, "root", keyFiles); //创建ssh连接对象
        ssh.Connect(); //  连接
        
        try
        {
            
            
            if (!zip)
            {
                ClearDirectory(sftp, ssh, airPath, tarPath, string.Empty);

                GetUploadDirectory(sftp, ssh, airPath, tarPath, string.Empty);
            }
            else
            {
                string zipLocalName = Path.GetFileNameWithoutExtension(airPath);

                string rad = GetRandomSeedbyGuid().ToString();
                zipLocalName = zipLocalName + rad;

                Console.WriteLine($"zipLocalName: {zipLocalName}");

                string sx = tarPath;
                string[] str = sx.Split('/'); //根据特定符号截取为字符串数组；
                string temp = str[str.Length - 1]; //取出数组最后一位；
                sx = sx.Substring(0, sx.Length - temp.Length - 1); //整个文件全名，去掉数据最后一位，剩下文件名；
                if (sx.IsNullOrEmpty())
                {
                    sx = "/";
                }

                ClearDirectory(sftp, ssh, airPath, tarPath, string.Empty);
                ClearDirectory(sftp, ssh, airPath, $"/{sx}/zip", string.Empty);

                //检查目标文件夹 是否存在, 不存在则创建
                MkdirPath(ssh, tarPath);

                MkdirPath(ssh, $"/{sx}/zip");

                Directory.SetCurrentDirectory(Directory.GetParent(airPath).FullName);
                string parentPath = Directory.GetCurrentDirectory();

                if (!Directory.Exists($"{parentPath}/zip"))
                {
                    Directory.CreateDirectory($"{parentPath}/zip");
                }
                else
                {
                    DirectoryInfo subdir = new DirectoryInfo($"{parentPath}/zip");
                    subdir.Delete(true); //删除子目录和文件
                    Directory.CreateDirectory($"{parentPath}/zip");
                }

                string zipName = $"{parentPath}/zip/{zipLocalName}.zip";
                CompressDirectory(airPath, zipName, 9, false);
                // ZipFileDictory(airPath, zipName);

                FileInfo t = new FileInfo(zipName); //获取文件
                zipSize = t.Length;

                SetFileUpload(sftp, ssh, $"{zipLocalName}.zip", zipName, $"/{sx}/zip/{zipLocalName}", "");

                SetCommand(ssh, $"cd /{sx}/zip/{zipLocalName}; unzip -o {zipLocalName}.zip  -d {tarPath}");

                SetCommand(ssh, $"chmod -R 755 {tarPath}");
            }
            
            
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        
        sftp.Disconnect(); //断开连接及文件流
        ssh.Disconnect();
    }

    #endregion

    #region CMD

    public void CMD(string str)
    {
        PrivateKeyFile keyFiles = new PrivateKeyFile(ConfigYml.keyFiles);

        var ssh = new SshClient(ConfigYml.SSH_IP, ConfigYml.SSH_Port, "root", keyFiles); //创建ssh连接对象
        ssh.Connect(); //  连接

        // SetCommand(ssh, str);

        if (!str.IsNullOrEmpty())
        {
            string[] arr = str.Split(";");

            foreach (var tr in arr)
            {
                string cm = tr.Replace("%", ";");

                // Console.Write(">> " + tr);
                SetCommand(ssh, cm);
            }
        }
        else
        {
            while (true) // 循环写入
            {
                Console.Write(">> ");
                var commed = Console.ReadLine(); // 读取指令

                if (string.IsNullOrEmpty(commed)) continue; //指令为空则continue
                if (string.Equals(commed, "exit")) break; //指令为“exit”退出循环

                var cmd = ssh.RunCommand(commed); //执行指令

                //var cmd = ssh.RunCommand(commed).Executed();  这种方式默认返回result,不能打印错误信息

                if (!string.IsNullOrEmpty(cmd.Error))
                {
                    Console.WriteLine("Error:\t" + cmd.Error + "\n"); //有错误信息则打印
                }
                else
                {
                    Console.WriteLine(cmd.Result); //打印结果
                }
            }
        }

        ssh.Disconnect(); //断开连接及文件流
    }

    #endregion


    #region ClearDirectory 清空指定目录下所有文件信息

    /// <summary>
    /// 清空指定目录下所有文件信息
    /// </summary>
    /// <param name="sftp"></param>
    /// <param name="ssh"></param>
    /// <param name="path"></param>
    /// <param name="tarPath"></param>
    /// <param name="subPath"></param>
    public void ClearDirectory(SftpClient sftp, SshClient ssh, string path, string tarPath, string subPath)
    {
        var commed = $"rm -rf {tarPath}";
        var cmd = ssh.RunCommand(commed); //执行指令

        if (!string.IsNullOrEmpty(cmd.Error))
        {
            Console.WriteLine("Error:\t" + cmd.Error + "\n"); //有错误信息则打印
            return;
        }
        else
        {
            Console.WriteLine(cmd.Result); //打印结果
            Console.WriteLine($"目标路径{tarPath}下所有文件清空"); //打印结果
        }
    }

    #endregion

    #region GetUploadDirectory

    public void GetUploadDirectory(SftpClient sftp, SshClient ssh, string path, string tarPath, string subPath)
    {
        //获取当前路径下的模板文件，指定文件进行处理
        DirectoryInfo root = new DirectoryInfo(path);
        FileInfo[] files = root.GetFiles();

        foreach (FileInfo fi in files)
        {
            SetFileUpload(sftp, ssh, fi.Name, fi.FullName, tarPath, subPath);
        }

        DirectoryInfo[] dir = root.GetDirectories();

        string sinPath = String.Empty;

        foreach (DirectoryInfo di in dir)
        {
            if (di.Name.Equals("tmp"))
            {
                continue;
            }

            if (!subPath.IsNullOrEmpty())
            {
                sinPath = subPath + "/" + di.Name;
            }
            else
            {
                sinPath = di.Name;
            }

            GetUploadDirectory(sftp, ssh, di.FullName, tarPath, sinPath);
        }
    }

    #endregion

    #region SetFileUpload

    public void SetFileUpload(SftpClient sftp, SshClient ssh, string name, string filePath, string tarPath,
        string subPath)
    {
        string tarFilePath = String.Empty;
        string tarDirPath = String.Empty;
        if (subPath.IsNullOrEmpty())
        {
            tarDirPath = $"{tarPath}";
            tarFilePath = $"{tarPath}/{name}";
        }
        else
        {
            tarDirPath = $"{tarPath}/{subPath}";
            tarFilePath = $"{tarPath}/{subPath}/{name}";
        }

        var commed = $"mkdir -p {tarDirPath}";
        var cmd = ssh.RunCommand(commed); //执行指令

        if (!string.IsNullOrEmpty(cmd.Error))
        {
            Console.WriteLine("Error:\t" + cmd.Error + "\n"); //有错误信息则打印
            return;
        }
        else
        {
            Console.WriteLine(cmd.Result); //打印结果
            Console.WriteLine("mkdir:" + tarDirPath); //打印结果
        }

        sftp.UploadFile(File.Open(filePath, FileMode.Open), tarFilePath, CL);
        Console.WriteLine("UploadFile:" + filePath + "~ to ~ " + tarFilePath); //打印结果

        var commed2 = $"chmod +x {tarFilePath}";
        var cmd2 = ssh.RunCommand(commed2); //执行指令

        if (!string.IsNullOrEmpty(cmd2.Error))
        {
            Console.WriteLine("Error:\t" + cmd2.Error + "\n"); //有错误信息则打印
            return;
        }
        else
        {
            Console.WriteLine("chmod:" + name); //打印结果
        }
    }

    public long zipSize;
    public long pl = 200;
    public long cu = 0;

    public void CL(ulong ui)
    {
        cu++;
        if (cu >= pl)
        {
            cu = 0;
            Console.WriteLine("curr:" + ui + " ~ all:" + zipSize);
        }
    }

    #endregion

    #region GetRandomSeedbyGuid

    /// <summary>
    /// 使用Guid生成种子
    /// </summary>
    /// <returns></returns>
    static int GetRandomSeedbyGuid()
    {
        return Guid.NewGuid().GetHashCode();
    }

    #endregion

    #region MkdirPath

    public void MkdirPath(SshClient ssh, string tarPath)
    {
        //判断目的文件是否存在
        // if [ ! -d "data/" ];then
        //     mkdir data
        // else
        // echo "dir exits"
        // fi

        string tarDirPath = String.Empty;
        tarDirPath = $"{tarPath}";

        var commed = $"mkdir -p {tarDirPath}";
        var cmd = ssh.RunCommand(commed); //执行指令

        if (!string.IsNullOrEmpty(cmd.Error))
        {
            Console.WriteLine("Error:\t" + cmd.Error + "\n"); //有错误信息则打印
        }
        else
        {
            Console.WriteLine(cmd.Result); //打印结果
            Console.WriteLine("mkdir:" + tarDirPath); //打印结果
        }
    }

    #endregion

    #region CompressDirectory

    /// <summary>
    /// 压缩文件夹
    /// </summary>
    /// <param name="dirPath">要打包的文件夹</param>
    /// <param name="GzipFileName">目标文件名</param>
    /// <param name="CompressionLevel">压缩品质级别（0~9）</param>
    /// <param name="deleteDir">是否删除原文件夹</param>
    public static void CompressDirectory(string dirPath, string GzipFileName, int CompressionLevel, bool deleteDir)
    {
        //压缩文件为空时默认与压缩文件夹同一级目录
        if (GzipFileName == string.Empty)
        {
            GzipFileName = dirPath.Substring(dirPath.LastIndexOf("\\"));
            GzipFileName = dirPath.Substring(0, dirPath.LastIndexOf("\\")) + "\\" + GzipFileName + ".zip";
        }

        try
        {
            using (ZipOutputStream zipoutputstream = new ZipOutputStream(File.Create(GzipFileName)))
            {
                //设置压缩文件级别
                zipoutputstream.SetLevel(CompressionLevel);
                Crc32 crc = new Crc32();
                Dictionary<string, DateTime> fileList = GetAllFies(dirPath);
                foreach (KeyValuePair<string, DateTime> item in fileList)
                {
                    //将文件数据读到流里面
                    FileStream fs = File.OpenRead(item.Key.ToString());
                    byte[] buffer = new byte[fs.Length];
                    //从流里读出来赋值给缓冲区
                    fs.Read(buffer, 0, buffer.Length);
                    ZipEntry entry = new ZipEntry(item.Key.Substring(dirPath.Length));
                    entry.DateTime = item.Value;
                    entry.Size = fs.Length;
                    fs.Close();
                    crc.Reset();
                    crc.Update(buffer);
                    entry.Crc = crc.Value;
                    zipoutputstream.PutNextEntry(entry);
                    zipoutputstream.Write(buffer, 0, buffer.Length);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    #endregion

    #region SetCommand

    public void SetCommand(SshClient ssh, string str)
    {
        Console.WriteLine(">> " + str); //打印结果
        var commed = str;
        var cmd = ssh.RunCommand(commed); //执行指令

        Console.WriteLine(cmd.Result); //打印结果
        Console.WriteLine(cmd.Error); //打印结果
        // if (!string.IsNullOrEmpty(cmd.Error))
        // {
        //     Console.WriteLine("Error:\t" + cmd.Error + "\n"); //有错误信息则打印
        //     return;
        // }
        // else
        // {
        //     Console.WriteLine(cmd.Result); //打印结果
        //     Console.WriteLine(str + " success"); //打印结果
        // }
    }

    #endregion

    #region GetAllFies

    /// <summary>
    /// 获取所有文件
    /// </summary>
    /// <returns></returns>
    private static Dictionary<string, DateTime> GetAllFies(string dir)
    {
        Dictionary<string, DateTime> FilesList = new Dictionary<string, DateTime>();
        DirectoryInfo fileDire = new DirectoryInfo(dir);
        if (!fileDire.Exists)
        {
            throw new System.IO.FileNotFoundException("目录:" + fileDire.FullName + "没有找到!");
        }

        // Console.WriteLine("目录:" +fileDire.FullName);
        //
        // if (fileDire.FullName.Equals(@"D:\WorkSpaces\DayByDay\DBD_Release\Server"))
        // {
        //     return FilesList;
        // }
        // else
        // {
        //     GetAllDirFiles(fileDire, FilesList);
        //     GetAllDirsFiles(fileDire.GetDirectories(), FilesList);
        //     return FilesList;
        // }

        GetAllDirFiles(fileDire, FilesList);
        GetAllDirsFiles(fileDire.GetDirectories(), FilesList);
        return FilesList;
    }

    #endregion

    #region GetAllDirFiles

    /// <summary>
    /// 获取一个文件夹下的文件
    /// </summary>
    /// <param name="dir">目录名称</param>
    /// <param name="filesList">文件列表HastTable</param>
    private static void GetAllDirFiles(DirectoryInfo dir, Dictionary<string, DateTime> filesList)
    {
        foreach (FileInfo file in dir.GetFiles("*.*"))
        {
            filesList.Add(file.FullName, file.LastWriteTime);
        }
    }

    #endregion

    #region GetAllDirsFiles

    /// <summary>
    /// 获取一个文件夹下的所有文件夹里的文件
    /// </summary>
    /// <param name="dirs"></param>
    /// <param name="filesList"></param>
    private static void GetAllDirsFiles(DirectoryInfo[] dirs, Dictionary<string, DateTime> filesList)
    {
        foreach (DirectoryInfo dir in dirs)
        {
            if (dir.Name.Equals("tmp"))
            {
                continue;
                ;
            }

            // Console.WriteLine("目录:" + dir.FullName);

            foreach (FileInfo file in dir.GetFiles("*.*"))
            {
                filesList.Add(file.FullName, file.LastWriteTime);
            }

            GetAllDirsFiles(dir.GetDirectories(), filesList);
        }
    }

    #endregion
}