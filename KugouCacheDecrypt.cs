using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    /// <summary>
    /// 酷狗缓存解密
    /// </summary>
    public class KugouCacheDecrypt : BaseCacheDecrypt
    {

        byte[] key = { 0xAC, 0xEC, 0xDF, 0x57 };

        public override string AcceptableExtension
        {
            get
            {
                return ".kgtemp";
            }
        }

        public override byte[] Decrypt(byte[] cacheFileData)
        {
            byte[] decodeData = new byte[cacheFileData.Length];

            for (var i = 0; i < cacheFileData.Length; i++)
            {
                var k = key[i % key.Length];
                var keyHigh = k >> 4;
                var keyLow = k & 0xf;
                var encryptionData = cacheFileData[i];
                var low = encryptionData & 0xf ^ keyLow;//解密后的低4位
                var high = (encryptionData >> 4) ^ keyHigh ^ low & 0xf;//解密后的高4位
                decodeData[i] = (byte)(high << 4 | low);
            }
            return decodeData;
        }


    }
}
