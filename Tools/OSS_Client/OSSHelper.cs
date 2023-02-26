using System;
using System.Collections.Generic;
using Aliyun.OSS;
using Aliyun.OSS.Common;

namespace Tools
{
    public class OSSHelper
    {
        #region OssClient

        public OssClient client;

        public OssClient OssClient()
        {
            if (client == null)
            {
                client = new OssClient(ConfigYml.Endpoint, ConfigYml.AccessKeyId, ConfigYml.AccessKeySecret);
            }

            return client;
        }

        #endregion

        #region put 上传文件, 支持文件增量上传

        /// <summary>
        /// 上传文件
        /// 支持 更新 删除 文件处理
        /// </summary>
        /// <param name="filePath">上传文件路径</param>
        /// <param name="projectName">项目根路径</param>
        /// <param name="bucketName">Bucket名称</param>
        public void Put(string filePath, string projectName, string bucketName)
        {
            Dictionary<string, HashObject> mem = new Dictionary<string, HashObject>();
            Dictionary<string, HashObject> cur = new Dictionary<string, HashObject>();
            List<HashObject> old = null;
            List<HashObject> ne = null;

            bool isUpdate = false;

            try
            {
                string txt = FileHelper.Load(ConfigYml.FileToPut);

                if (!txt.IsNullOrEmpty())
                {
                    old = JsonHelper.ToObject<List<HashObject>>(txt);
                }

                if (old != null)
                {
                    foreach (var oj in old)
                    {
                        mem.Add(oj.key, oj);
                    }
                }

                //获取文件路径
                List<string> List = DirectorHelper.GetDirector(filePath);

                foreach (var path in List)
                {
                    //hash 检查 判断是否已经存在 且 没有更新, 则不需要上传
                    string hashcode = FileHelper.GetHash(path);

                    if (mem.TryGetValue(path, out HashObject ob))
                    {
                        //有变更
                        if (!ob.value.Equals(hashcode))
                        {
                            cur.Add(path,
                                new HashObject() {key = path, value = hashcode, Change = true, Update = true});
                            isUpdate = true;
                        }
                        else
                        {
                            ob.Has = true;
                            cur.Add(path, new HashObject() {key = path, value = hashcode, Update = false, Has = true});
                        }
                    }
                    else
                    {
                        //新增
                        cur.Add(path, new HashObject() {key = path, value = hashcode, Add = true, Update = true});
                        isUpdate = true;
                    }
                }

                foreach (var hi in mem)
                {
                    if (hi.Value.Has)
                    {
                        continue;
                    }

                    if (!cur.ContainsKey(hi.Key))
                    {
                        //删除
                        cur.Add(hi.Key,
                            new HashObject() {key = hi.Key, value = hi.Value.value, Delete = true, Update = true});
                        isUpdate = true;
                    }
                }

                if (isUpdate)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("开始上传文件");
                    OssClient ossClient = OssClient();

                    int start = filePath.Length;

                    float allNum = cur.Count;
                    float curNum = 0;

                    int delNum = 0;
                    int addNum = 0;
                    int updateNum = 0;
                    int delErrNum = 0;
                    int addErrNum = 0;
                    int updateErrNum = 0;
                    int errStartNum = 5;

                    ConsoleColor colorBack = Console.BackgroundColor;
                    ConsoleColor colorFore = Console.ForegroundColor;

                    foreach (var cu in cur)
                    {
                        curNum++;

                        Console.ForegroundColor = ConsoleColor.Cyan; //设置进度条颜色
                        Console.SetCursorPosition((int) curNum / 4 + 1, 2); //设置光标位置,参数为第几列和第几行
                        Console.Write("{0}%", (int) (curNum / allNum * 100));
                        Console.ForegroundColor = colorBack; //恢复输出颜色

                        Console.BackgroundColor = ConsoleColor.Cyan; //设置进度条颜色
                        Console.SetCursorPosition((int) curNum / 4, 2); //设置光标位置,参数为第几列和第几行
                        Console.Write(" "); //移动进度条
                        Console.BackgroundColor = colorBack; //恢复输出颜色

                        if (cu.Value.Update)
                        {
                            string key = cu.Value.key.Substring(start);
                            key = projectName + key;
                            key = key.Replace(@"\", "/");

                            if (!cu.Value.Delete)
                            {
                                try
                                {
                                    ossClient.PutObject(bucketName, key, cu.Value.key);

                                    if (cu.Value.Add)
                                    {
                                        addNum++;
                                    }

                                    if (cu.Value.Change)
                                    {
                                        updateNum++;
                                    }

                                    Console.ForegroundColor = ConsoleColor.Green; //设置进度条颜色
                                    Console.SetCursorPosition(0, 3); //设置光标位置,参数为第几列和第几行
                                    Console.Write("总成功 新增文件数量: {0} - 更新文件数量: {1}, 移除文件数量: {2}", addNum, updateNum,
                                        delNum);
                                    Console.BackgroundColor = colorBack; //恢复输出颜色

                                    Console.ForegroundColor = ConsoleColor.Red; //设置进度条颜色
                                    Console.SetCursorPosition(0, 4); //设置光标位置,参数为第几列和第几行
                                    Console.Write("总失败 新增文件数量: {0} - 更新文件数量: {1}, 移除文件数量: {2}", addErrNum, updateErrNum,
                                        delErrNum);
                                    Console.ForegroundColor = colorBack; //恢复输出颜色

                                    Console.ForegroundColor = ConsoleColor.White; //设置进度条颜色
                                    Console.SetCursorPosition(0, 5);
                                    Console.Write("Put object:{0} succeeded", key);
                                    Console.ForegroundColor = colorFore;
                                }
                                catch (OssException ex)
                                {
                                    errStartNum++;

                                    if (cu.Value.Add)
                                    {
                                        addErrNum++;
                                    }

                                    if (cu.Value.Change)
                                    {
                                        updateErrNum++;
                                    }

                                    Console.ForegroundColor = ConsoleColor.Red; //设置进度条颜色
                                    Console.SetCursorPosition(0, errStartNum); //设置光标位置,参数为第几列和第几行
                                    Console.WriteLine(
                                        "Failed with error code: {0}; Error info: {1}. \nRequestID:{2}\tHostID:{3}",
                                        ex.ErrorCode, ex.Message, ex.RequestId, ex.HostId);
                                    Console.ForegroundColor = colorBack; //恢复输出颜色
                                }
                                catch (Exception ex)
                                {
                                    errStartNum++;

                                    if (cu.Value.Add)
                                    {
                                        addErrNum++;
                                    }

                                    if (cu.Value.Change)
                                    {
                                        updateErrNum++;
                                    }

                                    Console.ForegroundColor = ConsoleColor.Red; //设置进度条颜色
                                    Console.SetCursorPosition(0, errStartNum); //设置光标位置,参数为第几列和第几行
                                    Console.WriteLine("Failed with error info: {0}", ex.Message);
                                    Console.ForegroundColor = colorBack; //恢复输出颜色
                                }
                            }
                            else
                            {
                                try
                                {
                                    client.DeleteObject(bucketName, key);

                                    delNum++;

                                    Console.ForegroundColor = ConsoleColor.Green; //设置进度条颜色
                                    Console.SetCursorPosition(0, 3); //设置光标位置,参数为第几列和第几行
                                    Console.Write("总成功 新增文件数量: {0} - 更新文件数量: {1}, 移除文件数量: {2}", addNum, updateNum,
                                        delNum);
                                    Console.BackgroundColor = colorBack; //恢复输出颜色

                                    Console.ForegroundColor = ConsoleColor.Red; //设置进度条颜色
                                    Console.SetCursorPosition(0, 4); //设置光标位置,参数为第几列和第几行
                                    Console.Write("总失败 新增文件数量: {0} - 更新文件数量: {1}, 移除文件数量: {2}", addErrNum, updateErrNum,
                                        delErrNum);
                                    Console.ForegroundColor = colorBack; //恢复输出颜色

                                    Console.ForegroundColor = ConsoleColor.White; //设置进度条颜色
                                    Console.SetCursorPosition(0, 5);
                                    Console.Write("Delete object:{0} succeeded", key);
                                    Console.ForegroundColor = colorFore;
                                }
                                catch (OssException ex)
                                {
                                    errStartNum++;
                                    delErrNum++;

                                    Console.ForegroundColor = ConsoleColor.Red; //设置进度条颜色
                                    Console.SetCursorPosition(0, errStartNum); //设置光标位置,参数为第几列和第几行
                                    Console.WriteLine(
                                        "Failed with error code: {0}; Error info: {1}. \nRequestID:{2}\tHostID:{3}",
                                        ex.ErrorCode, ex.Message, ex.RequestId, ex.HostId);
                                    Console.ForegroundColor = colorBack; //恢复输出颜色
                                }
                                catch (Exception ex)
                                {
                                    errStartNum++;
                                    delErrNum++;

                                    Console.ForegroundColor = ConsoleColor.Red; //设置进度条颜色
                                    Console.SetCursorPosition(0, errStartNum); //设置光标位置,参数为第几列和第几行
                                    Console.WriteLine("Failed with error info: {0}", ex.Message);
                                    Console.ForegroundColor = colorBack; //恢复输出颜色
                                }
                            }
                        }
                    }

                    errStartNum++;
                    Console.SetCursorPosition(0, errStartNum); //设置光标位置,参数为第几列和第几行
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("上传文件结束");
                }


                //传输成功 需要 另存为
                ne = new List<HashObject>();
                foreach (var cu in cur)
                {
                    if (!cu.Value.Delete)
                    {
                        ne.Add(cu.Value);
                    }
                }

                if (ne.Count > 0)
                {
                    string json = JsonHelper.ToJson(ne);
                    FileHelper.Save(json, ConfigYml.FileToPut);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        #endregion
    }
}