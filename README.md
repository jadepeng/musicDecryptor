# musicDecryptor
酷狗、网易等音乐缓存文件转mp3

以前写过[kgtemp文件转mp3工具](http://www.cnblogs.com/xiaoqi/p/8085563.html),正好当前又有网易云音乐缓存文件需求，因此就在原来小工具的基础上做了一点修改，增加了对网易云音乐的支持，并简单调整了下代码结构，方便后续增加其他音乐软件的支持。
PS:写惯了java，再写c#好别扭。。


## 工具使用介绍

启动程序，

![enter description here][1]

- 首先，设置输入目录，也就是解密后的文件存放在哪里
- 然后将酷狗或者网易的缓存文件 or 整个文件夹，拖入到程序即可

![enter description here][2]
打开转码结果目录，可以看到转码后的结果

![enter description here][3]

## FAQ

### 网易云音乐的缓存目录

打开设置 -- 下载设置 - 缓存目录就是了

![enter description here][4]


### 酷狗缓存目录

如图，在设置--下载设置里

![enter description here][5]


## 工具代码简要说明

### 类图

![enter description here][6]


### ICacheDecrypt
我们定义一个解码接口ICacheDecrypt，实现将缓存文件字节流转换为mp3字节流。

``` cs
 /// <summary>
    /// 解密接口
    /// </summary>
    public interface ICacheDecrypt
    {

        string AcceptableExtension
        {
            get;
        }

        bool isAcceptable(string cacheFile);

        /// <summary>
        /// 解密文件
        /// </summary>
        /// <param name="cacheFile">缓存文件</param>
        /// <returns>解密后二进制数据</returns>
        byte[] Decrypt(string cacheFile);

        /// <summary>
        /// 解密文件
        /// </summary>
        /// <param name="cacheFileData">缓存文件数据</param>
        /// <returns></returns>
        byte[] Decrypt(byte[] cacheFileData);

        /// <summary>
        /// 解密文件
        /// </summary>
        /// <param name="cacheFile">cache文件</param>
        /// <param name="decodedFile">解密后文件</param>
        void Decrypt(string cacheFile,string decodedFile);

    }
```

### BaseCacheDecrypt

然后，实现一个默认的抽象类BaseCacheDecrypt，实现一些公共的东西，具体的转码工作让子类去实现：

``` cs
public abstract class BaseCacheDecrypt : ICacheDecrypt
    {

        protected string currentCacheFile;

        public abstract string AcceptableExtension
        {
            get;
        }

        public abstract byte[] Decrypt(byte[] cacheFileData);

        public byte[] Decrypt(string cacheFile)
        {
            currentCacheFile = cacheFile;
            return Decrypt(File.ReadAllBytes(cacheFile));
        }

        public void Decrypt(string cacheFile, string decodedFile)
        {
            File.WriteAllBytes(decodedFile, Decrypt(cacheFile));
        }

        public  bool isAcceptable(string cacheFile)
        {
            return cacheFile.EndsWith(AcceptableExtension);
        }
    }
```

### NetMusicCacheDecrypt
然后，分别实现酷狗和网易云音乐的解码工作，酷狗的上次已经写了如何解码，这里只贴网易的，解码很简单，异或0xa3就可以了。网易音乐在测试时发现好多mp3没有ID3信息，经过观察发现缓存文件名里包含歌曲的id信息，因此可以根据这个id信息去抓取歌曲网页，解析出歌手和歌曲名称，然后写入到ID3里，这里ID3的读写采用了GitHub上的一个开源库


``` cs

    /// <summary>
    /// 网易云缓存解密
    /// </summary>
    public class NetMusicCacheDecrypt : BaseCacheDecrypt
    {
        public override string AcceptableExtension
        {
            get
            {
                return ".uc";
            }
        }

        string cut(string str,string start,string end)
        {
            var startIndex = str.IndexOf(start);
            if (startIndex == -1)
            {
                return "";
            }
            startIndex += start.Length;
            var endIndex = str.IndexOf(end, startIndex);
            if (endIndex == -1)
            {
                return "";
            }
            return str.Substring(startIndex, endIndex - startIndex);
        }

        public override byte[] Decrypt(byte[] cacheFileData)
        {         
            for (var i = 0; i < cacheFileData.Length; i++)
            {
                // 异或0xa3
                cacheFileData[i] ^= 0xa3;
            }

            var fileName = new FileInfo(currentCacheFile).Name;
            var songId = fileName.Substring(0, fileName.IndexOf("-"));
            var html = HttpHelper.SendGet("http://music.163.com/song?id=" + songId);
            if (html.Length > 0)
            {
                var title = cut(html, "<title>", "</title>").Trim();
                var tempFile = currentCacheFile+ Guid.NewGuid().ToString();
                File.WriteAllBytes(tempFile, cacheFileData);
                Track theTrack = new Track(tempFile);
                // 父亲写的散文诗(时光版) - 许飞 - 单曲 - 网易云音乐
                theTrack.Artist = cut(title, "-", "-").Trim();
                theTrack.Title = title.Substring(0, title.IndexOf("-")).Trim();
                // Save modifications on the disc
                theTrack.Save();
                cacheFileData = File.ReadAllBytes(tempFile);
                File.Delete(tempFile);

            }
            
            return cacheFileData;
        }

    }
```

接着介绍核心的Decryptor，实现转码的调度，这里的思路就是将所有的解码器放到一个list里，当一个文件过来的时候，遍历所有解码器，如果accetbale，就处理，否则跳过。
两个主要工作：
-   加载所有的BaseCacheDecrypt
-   进行解码工作

### 加载所有的BaseCacheDecrypt

两种方法，一是自己实例化，一是使用反射，这里当然用反射了：）

``` cs

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
```

Decryptor通过单例模式对外提供调用。

### 进行解码

判断拖入的是文件夹还是文件，文件夹的话遍历子文件，依次处理。解码方式就是钢说的，遍历decryptors，如果支持就解码。
解码完后，读取ID3信息，对文件进行重命名。

``` cs
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
```

## 开源地址

代码托管到了GitHub，[musicDecryptor](https://github.com/jadepeng/musicDecryptor), 感兴趣的可以访问进行


---
>作者：Jadepeng
出处：jqpeng的技术记事本--[http://www.cnblogs.com/xiaoqi](http://www.cnblogs.com/xiaoqi)
您的支持是对博主最大的鼓励，感谢您的认真阅读。
本文版权归作者所有，欢迎转载，但未经作者同意必须保留此段声明，且在文章页面明显位置给出原文连接，否则保留追究法律责任的权利。


  [1]: http://oyqmmpkcm.bkt.clouddn.com/1522310112266.jpg "1522310112266"
  [2]: http://oyqmmpkcm.bkt.clouddn.com/1522310257994.jpg "1522310257994"
  [3]: http://oyqmmpkcm.bkt.clouddn.com/1522310276416.jpg "1522310276416"
  [4]: http://oyqmmpkcm.bkt.clouddn.com/1522310348535.jpg "1522310348535"
  [5]: http://oyqmmpkcm.bkt.clouddn.com/1522310427084.jpg "1522310427084"
  [6]: http://oyqmmpkcm.bkt.clouddn.com/1522310518946.jpg "1522310518946"
