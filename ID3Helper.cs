using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using ATL.AudioData;

namespace WindowsFormsApp1
{

    public class ID3Info
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Year { get; set; }
        public string Comment { get; set; }
    }

    public class ID3Helper

    {
        #region MP3信息结构
        /// <summary>
        /// MP3信息结构
        /// </summary>
        public struct Mp3Info
        {
            public string identify; //TAG，三个字节
            public string Title; //歌曲名,30个字节
            public string Artist; //歌手名,30个字节
            public string Album; //所属唱片,30个字节
            public string Year; //年,4个字符
            public string Comment; //注释,28个字节
            public char reserved1; //保留位，一个字节
            public char reserved2; //保留位，一个字节
            public char reserved3; //保留位，一个字节
        }
        #endregion

        public static Encoding DetectEncoding(byte encoding = 0x00)
        {
            Encoding tagEncoding = null;
            // Checks to see what encoding type it is.
            switch (encoding)
            {
                case 0x00: tagEncoding = Encoding.GetEncoding("ISO-8859-1"); break;
                case 0x01: tagEncoding = Encoding.GetEncoding("UTF-16"); break;
                case 0x02: tagEncoding = Encoding.GetEncoding("UTF-16BE"); break;
                case 0x03: tagEncoding = Encoding.UTF8; break;
                default: throw new Exception("Invalid encoding type of ID3v2.");
            }
            return tagEncoding;
        }

        public static Mp3Info GetMp3Info(string FileName)
        {
            //打开文件
            FileStream fs = new FileStream(FileName, FileMode.Open, FileAccess.Read);

            Encoding FileEncoding = Encoding.GetEncoding("GBK");
            //获取MP3文件最后128个字节,ID3信息保存于此,如果获取失败,则返回null
            const int seekPos = 128;
            fs.Seek(-seekPos, SeekOrigin.End); //从文件尾部开始往回seek到128字节处
            int rl = 0;
            byte[] Last128 = new byte[seekPos];
            rl = fs.Read(Last128, 0, seekPos); //将最后的128个字节读出来放入byte[]中
            fs.Seek(0, SeekOrigin.Begin);  //恢复Seek位置
            //关闭文件
            fs.Close();

            //将mp3最后的128个字节格式化为Mp3Info
            Mp3Info myMp3Info = FormatMp3Info(Last128, FileEncoding);
            //返回
            return myMp3Info;
        }

        #region 将mp3最后的128个字节格式化为Mp3Info

        /// <summary>
        /// 将mp3最后的128个字节格式化为Mp3Info
        /// </summary>
        /// <param name = "Info">从MP3文件中截取的二进制信息</param>
        /// <returns>返回一个Mp3Info结构</returns>
        private static Mp3Info FormatMp3Info(byte[] Info, System.Text.Encoding Encoding)
        {
            Mp3Info myMp3Info = new Mp3Info();
            string str = null;
            int i;
            int position = 0; //循环的起始值
            int currentIndex = 0; //Info的当前索引值

            //获取TAG标识
            for (i = currentIndex; i < currentIndex + 3; i++)
            {
                str = str + (char)Info[i];
                position++;
            }
            currentIndex = position;
            myMp3Info.identify = str;

            //获取歌名
            str = null;
            byte[] bytTitle = new byte[30]; //将歌名部分读到一个单独的数组中
            int j = 0;
            for (i = currentIndex; i < currentIndex + 30; i++)
            {
                bytTitle[j] = Info[i];
                position++;
                j++;
            }
            currentIndex = position;
            myMp3Info.Title = ByteToString(bytTitle, Encoding);

            //获取歌手名
            str = null;
            j = 0;
            byte[] bytArtist = new byte[30]; //将歌手名部分读到一个单独的数组中
            for (i = currentIndex; i < currentIndex + 30; i++)
            {
                bytArtist[j] = Info[i];
                position++;
                j++;
            }
            currentIndex = position;
            myMp3Info.Artist = ByteToString(bytArtist, Encoding);

            //获取唱片名
            str = null;
            j = 0;
            byte[] bytAlbum = new byte[30]; //将唱片名部分读到一个单独的数组中
            for (i = currentIndex; i < currentIndex + 30; i++)
            {
                bytAlbum[j] = Info[i];
                position++;
                j++;
            }
            currentIndex = position;
            myMp3Info.Album = ByteToString(bytAlbum, Encoding);

            //获取年
            str = null;
            j = 0;
            byte[] bytYear = new byte[4]; //将年部分读到一个单独的数组中
            for (i = currentIndex; i < currentIndex + 4; i++)
            {
                bytYear[j] = Info[i];
                position++;
                j++;
            }
            currentIndex = position;
            myMp3Info.Year = ByteToString(bytYear, Encoding);

            //获取注释
            str = null;
            j = 0;
            byte[] bytComment = new byte[28]; //将注释部分读到一个单独的数组中
            for (i = currentIndex; i < currentIndex + 25; i++)
            {
                bytComment[j] = Info[i];
                position++;
                j++;
            }
            currentIndex = position;
            myMp3Info.Comment = ByteToString(bytComment, Encoding);

            //以下获取保留位
            myMp3Info.reserved1 = (char)Info[++position];
            myMp3Info.reserved2 = (char)Info[++position];
            myMp3Info.reserved3 = (char)Info[++position];

            //
            return myMp3Info;
        }
        #endregion

        /// <summary>
        /// 将字节数组转换成字符串
        /// </summary>
        /// <param name = "b">字节数组</param>
        /// <returns>返回转换后的字符串</returns>
        public static string ByteToString(byte[] SourceByte, System.Text.Encoding Encoding)
        {
            string str = Encoding.GetString(SourceByte);

            //去掉无用字符
            str = str.Substring(0, str.IndexOf('\0') >= 0 ? str.IndexOf('\0') : str.Length);

            return str;
        }

        //以下为读取id3v2，源码来自http://www.cnblogs.com/wscar/p/6362790.html
        public static Mp3Info ReadMp3(string path)
        {

            AudioDataManager theFile = new AudioDataManager(ATL.AudioData.AudioDataIOFactory.GetInstance().GetDataReader(path));
            theFile.ReadFromFile(true, true);
          

            if (theFile.ID3v2 != null && theFile.ID3v2.Title.Length > 0)
            {
                return new Mp3Info()
                {
                    Title = theFile.ID3v2.Title,
                    Artist = theFile.ID3v2.Artist
                };
            }

            return new Mp3Info()
            {
                Title = theFile.ID3v1.Title,
                Artist = theFile.ID3v1.Artist
            };


            //Mp3Info myMp3Info = GetMp3Info(path);

            //if (!String.IsNullOrEmpty(myMp3Info.Title))
            //{
            //    return myMp3Info;
            //}

            //string[] tags = new string[6];
            //FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            //byte[] buffer = new byte[10];
            //// fs.Read(buffer, 0, 128);
            //string mp3ID = "";

            //fs.Seek(0, SeekOrigin.Begin);
            //fs.Read(buffer, 0, 10);
            //int size = (buffer[6] & 0x7F) * 0x200000 + (buffer[7] & 0x7F) * 0x400 + (buffer[8] & 0x7F) * 0x80 + (buffer[9] & 0x7F);
            ////int size = (buffer[6] & 0x7F) * 0X200000 * (buffer[7] & 0x7f) * 0x400 + (buffer[8] & 0x7F) * 0x80 + (buffer[9]);
            //mp3ID = Encoding.Default.GetString(buffer, 0, 3);
            //if (mp3ID.Equals("ID3", StringComparison.OrdinalIgnoreCase))
            //{                
            //    //如果有扩展标签头就跨过 10个字节
            //    if ((buffer[5] & 0x40) == 0x40)
            //    {
            //        fs.Seek(10, SeekOrigin.Current);
            //        size -= 10;
            //    }
            //    tags = ReadFrame(fs, size);
            //    myMp3Info.Title = tags[0];
            //    myMp3Info.Artist = tags[1];
            //    myMp3Info.Album = tags[2];
            //    myMp3Info.Year = tags[3];
            //    myMp3Info.Comment = tags[4];
            //}
            //else
            //    myMp3Info = GetMp3Info(path);
            //fs.Close();
            //return myMp3Info;
        }
        public static string[] ReadFrame(FileStream fs, int size)
        {
            string[] ID3V2 = new string[6];
            byte[] buffer = new byte[10];
            while (size > 0)
            {
                //fs.Read(buffer, 0, 1);
                //if (buffer[0] == 0)
                //{
                //    size--;
                //    continue;
                //}
                //fs.Seek(-1, SeekOrigin.Current);
                //size++;
                //读取标签帧头的10个字节
                fs.Read(buffer, 0, 10);
                size -= 10;
                //得到标签帧ID
                string FramID = Encoding.Default.GetString(buffer, 0, 4);
                //计算标签帧大小，第一个字节代表帧的编码方式
                int frmSize = 0;

                frmSize = buffer[4] * 0x1000000 + buffer[5] * 0x10000 + buffer[6] * 0x100 + buffer[7];
                if (frmSize == 0)
                {
                    //就说明真的没有信息了
                    break;
                }
                //bFrame 用来保存帧的信息
                byte[] bFrame = new byte[frmSize];
                fs.Read(bFrame, 0, frmSize);
                size -= frmSize;
                string str = GetFrameInfoByEcoding(bFrame, bFrame[0], frmSize - 1);
                if (FramID.CompareTo("TIT2") == 0)
                {
                    ID3V2[0] =  str;
                }
                else if (FramID.CompareTo("TPE1") == 0)
                {
                    ID3V2[1] =  str;
                }
                else if (FramID.CompareTo("TALB") == 0)
                {
                    ID3V2[2] =  str;
                }
                else if (FramID.CompareTo("TIME") == 0)
                {
                    ID3V2[3] =  str;
                }
                else if (FramID.CompareTo("COMM") == 0)
                {
                    ID3V2[4] =  str;
                }
            }
            return ID3V2;
        }
        public static string GetFrameInfoByEcoding(byte[] b, byte conde, int length)
        {
            string str = "";
            switch (conde)
            {
                case 0:
                    str = Encoding.GetEncoding("ISO-8859-1").GetString(b, 1, length);
                    break;
                case 1:
                    str = Encoding.GetEncoding("UTF-16LE").GetString(b, 1, length);
                    break;
                case 2:
                    str = Encoding.GetEncoding("UTF-16BE").GetString(b, 1, length);
                    break;
                case 3:
                    str = Encoding.UTF8.GetString(b, 1, length);
                    break;
            }
            return str;
        }
    }
}
