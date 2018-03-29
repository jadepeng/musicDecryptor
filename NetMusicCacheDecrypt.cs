using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ATL.AudioData;
using ATL;
using System.IO;

namespace WindowsFormsApp1
{
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
}
