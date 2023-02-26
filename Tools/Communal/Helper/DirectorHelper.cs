using System;
using System.Collections.Generic;
using System.IO;

namespace Tools
{
    public static class DirectorHelper
    {
        public static List<string> GetDirector(string dirs)
        {
            List<string> List = new List<string>();

            Director(dirs, List);

            return List;
        }

        public static void Director(string dirs, List<string> List)
        {
            //绑定到指定的文件夹目录
            DirectoryInfo dir = new DirectoryInfo(dirs);
            //检索表示当前目录的文件和子目录
            FileSystemInfo[] fsinfos = dir.GetFileSystemInfos();
            //遍历检索的文件和子目录
            foreach (FileSystemInfo fsinfo in fsinfos)
            {
                //判断是否为空文件夹　　
                if (fsinfo is DirectoryInfo)
                {
                    //递归调用
                    Director(fsinfo.FullName, List);
                }
                else
                {
                    //将得到的文件全路径放入到集合中
                    List.Add(fsinfo.FullName);
                }
            }
        }
    }
}