using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
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
}
