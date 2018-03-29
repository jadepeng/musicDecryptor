using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{

    /// <summary>
    /// 解密器
    /// </summary>
    class Decryptor
    {

        private ILog _logger = LogManager.GetLogger(typeof(Decryptor));

        List<ICacheDecrypt> cacheDecryptors = new List<ICacheDecrypt>();

        /// <summary>
        /// 自动读取mp3信息重命名
        /// </summary>
        public bool AutoRename{ get; set; } = true;

        /// <summary>
        /// 目标目录
        /// </summary>
        public string TargetDirectory { get; set; } = "";

        private Decryptor()
        {
           
        }

        public static Decryptor Instance
        {
            get
            {
                return Holder.decryptor;
            }
        }

        private string GetAcceptedFilePattern()
        {
            StringBuilder rBuilder = new StringBuilder();
            var isFirst = true;
            foreach (var decryptor in cacheDecryptors)
            {
                if (!isFirst)
                {
                    rBuilder.Append("|");
                }
                rBuilder.Append("*").Append(decryptor.AcceptableExtension);
                isFirst = false;
            }
            return rBuilder.ToString();
        }

        public int Process(string path)
        {
            int success = 0;

            if (Directory.Exists(path))//如果是文件夹
            {
                DirectoryInfo dinfo = new DirectoryInfo(path);//实例化一个DirectoryInfo对象
                foreach (FileInfo fs in dinfo.GetFiles()) //查找.kgtemp文件
                {
                    ProcessFile(fs.FullName);
                    success++;
                }
            }
            else
            {
                ProcessFile(path);
                success = 1;
            }

            return success;
        }

        private string GetCleanFileName(string fileName)
        {
            StringBuilder rBuilder = new StringBuilder(fileName);
            foreach (char rInvalidChar in Path.GetInvalidFileNameChars())
                rBuilder.Replace(rInvalidChar.ToString(), string.Empty);
            return rBuilder.ToString();
        }

        private string GetTargetFileName(string fileName)
        {
            var fileinfo = new FileInfo(fileName);
            var rawName = fileinfo.Name.Substring(0, fileinfo.Name.IndexOf("."));
            return TargetDirectory + Path.DirectorySeparatorChar + rawName + ".mp3";
        }


        void ProcessFile(string fileName)
        {
            _logger.Info("开始处理" + fileName);
            try
            {
                foreach (var decryptor in cacheDecryptors)
                {
                    if (decryptor.isAcceptable(fileName))
                    {
                        var targetName = TargetDirectory + Path.DirectorySeparatorChar + new FileInfo(fileName).Name + ".mp3";

                        decryptor.Decrypt(fileName, targetName);

                        // 重命名
                        if (AutoRename)
                        {
                            var mp3 = ID3Helper.ReadMp3(targetName);

                            if (mp3.Title.Length > 0)
                            {
                                string realFileName = GetTargetFileName(GetCleanFileName(mp3.Title + "-" + mp3.Artist + ".mp3"));

                                _logger.Info("重命名" + realFileName);
                                if (File.Exists(realFileName))
                                {
                                    File.Delete(realFileName);
                                }

                                File.Move(targetName, realFileName);
                            }
                        }
                    }
                }
                _logger.Info(fileName + "处理完成");
            }
            catch(Exception ex)
            {
                _logger.Error(fileName + "出现异常" + ex.Message);
            }
          
        }


        static class Holder
        {
            public static Decryptor decryptor = Load();


            /// <summary>
            /// 从当前Assembly加载
            /// </summary>
            /// <returns></returns>
            private static Decryptor Load()
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                List<Type> hostTypes = new List<Type>();

                foreach (var type in assembly.GetExportedTypes())
                {
                    //确定type为类并且继承自(实现)IMyInstance
                    if (type.IsClass && typeof(BaseCacheDecrypt).IsAssignableFrom(type) && !type.IsAbstract)
                        hostTypes.Add(type);
                }

                Decryptor decryptor = new Decryptor();
                foreach (var type in hostTypes)
                {
                    ICacheDecrypt instance = (ICacheDecrypt)Activator.CreateInstance(type);
                    decryptor.cacheDecryptors.Add(instance);
                }

                return decryptor;
            }
        }
    }
}
