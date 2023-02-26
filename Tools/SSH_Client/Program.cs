namespace Tools
{
    class Program
    {
        static void Main(string[] args)
        {
            
            // sftp_ip ip地址
            // sftp_port ip端口
            // key_path 秘钥 用于连接ssh, 本地秘钥存放路径
            // 1 cmd执行命令行 2 push 上传文件
            // airPath 原文件路径
            // tarPath 目标文件路径
            // "true" 是否压缩上传

            // 1、上传文件
            // string cmd = sftp_ip + " " + sftp_port + " " + key_path + " " + 2 + " " + airPath + " " + tarPath + " " + "true";
            //
            // 2、执行命令行
            // string cmd = sftp_ip + " " + sftp_port + " " + key_path + " " + 1 + " " + $"cd {sh_Path}%  dos2unix drone.sh%  ./drone.sh" ;
            
            
            if (args.Length > 0)
            {
                //这个工具类 主要是用来 简化相关的命令行敲写 
                
                //前4项固定， 根据 CommandType 来判断执行命令还是上传
                ConfigYml.SSH_IP = args[0];
                ConfigYml.SSH_Port = int.Parse(args[1]);
                ConfigYml.keyFiles = args[2];
                ConfigYml.CommandType = (CommandType)int.Parse(args[3]);

                if (ConfigYml.CommandType == CommandType.CMD)
                {
                    //执行命令行
                    
                    for (int i = 4; i < args.Length; i++)
                    {
                        ConfigYml.CMD = ConfigYml.CMD + " " + args[i];
                    }
                    SSHHelper ssh = new SSHHelper();
                    
                    ssh.CMD(ConfigYml.CMD);
                }
                else
                {
                    //执行文件上传功能
                    
                    ConfigYml.AirPath = args[4];
                    ConfigYml.TarPath = args[5];
                    ConfigYml.Zip = bool.Parse(args[6]);

                    SSHHelper ssh = new SSHHelper();
                    ssh.SFTP(ConfigYml.AirPath, ConfigYml.TarPath, ConfigYml.Zip);
                }
                
            }

        }
    }
}