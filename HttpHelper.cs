using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.IO.Compression;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace WindowsFormsApp1
{
    public class HttpHelper
    {
        private static bool RemoteCertificateValidate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            //用户https请求
            return true; //总是接受
        }

        public static string SendPost(string url, string data)
        {
            return Send(url, "POST", data, null);
        }

        public static string SendGet(string url)
        {
            return Send(url, "GET", null, null);
        }

        public static string Send(string url, string method, string data, HttpConfig config)
        {
            if (config == null) config = new HttpConfig();
            string result;
            using (HttpWebResponse response = GetResponse(url, method, data, config))
            {
                Stream stream = response.GetResponseStream();

                if (!String.IsNullOrEmpty(response.ContentEncoding))
                {
                    if (response.ContentEncoding.Contains("gzip"))
                    {
                        stream = new GZipStream(stream, CompressionMode.Decompress);
                    }
                    else if (response.ContentEncoding.Contains("deflate"))
                    {
                        stream = new DeflateStream(stream, CompressionMode.Decompress);
                    }
                }

                byte[] bytes = null;
                using (MemoryStream ms = new MemoryStream())
                {
                    int count;
                    byte[] buffer = new byte[4096];
                    while ((count = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        ms.Write(buffer, 0, count);
                    }
                    bytes = ms.ToArray();
                }

                #region 检测流编码
                Encoding encoding;

                //检测响应头是否返回了编码类型,若返回了编码类型则使用返回的编码
                //注：有时响应头没有编码类型，CharacterSet经常设置为ISO-8859-1
                if (!string.IsNullOrEmpty(response.CharacterSet) && response.CharacterSet.ToUpper() != "ISO-8859-1")
                {
                    encoding = Encoding.GetEncoding(response.CharacterSet == "utf8" ? "utf-8" : response.CharacterSet);
                }
                else
                {
                    //若没有在响应头找到编码，则去html找meta头的charset
                    result = Encoding.Default.GetString(bytes);
                    //在返回的html里使用正则匹配页面编码
                    Match match = Regex.Match(result, @"<meta.*charset=""?([\w-]+)""?.*>", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        encoding = Encoding.GetEncoding(match.Groups[1].Value);
                    }
                    else
                    {
                        //若html里面也找不到编码，默认使用utf-8
                        encoding = Encoding.GetEncoding(config.CharacterSet);
                    }
                }
                #endregion

                result = encoding.GetString(bytes);
            }
            return result;
        }

        private static HttpWebResponse GetResponse(string url, string method, string data, HttpConfig config)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method;
            request.Referer = config.Referer;
            //有些页面不设置用户代理信息则会抓取不到内容
            request.UserAgent = config.UserAgent;
            request.Timeout = config.Timeout;
            request.Accept = config.Accept;
            request.Headers.Set("Accept-Encoding", config.AcceptEncoding);
            request.ContentType = config.ContentType;
            request.KeepAlive = config.KeepAlive;

            if (url.ToLower().StartsWith("https"))
            {
                //这里加入解决生产环境访问https的问题--Could not establish trust relationship for the SSL/TLS secure channel
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(RemoteCertificateValidate);
            }


            if (method.ToUpper() == "POST")
            {
                if (!string.IsNullOrEmpty(data))
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(data);

                    if (config.GZipCompress)
                    {
                        using (MemoryStream stream = new MemoryStream())
                        {
                            using (GZipStream gZipStream = new GZipStream(stream, CompressionMode.Compress))
                            {
                                gZipStream.Write(bytes, 0, bytes.Length);
                            }
                            bytes = stream.ToArray();
                        }
                    }

                    request.ContentLength = bytes.Length;
                    request.GetRequestStream().Write(bytes, 0, bytes.Length);
                }
                else
                {
                    request.ContentLength = 0;
                }
            }

            return (HttpWebResponse)request.GetResponse();
        }
    }

    public class HttpConfig
    {
        public string Referer { get; set; }

        /// <summary>
        /// 默认(text/html)
        /// </summary>
        public string ContentType { get; set; }

        public string Accept { get; set; }

        public string AcceptEncoding { get; set; }

        /// <summary>
        /// 超时时间(毫秒)默认100000
        /// </summary>
        public int Timeout { get; set; }

        public string UserAgent { get; set; }

        /// <summary>
        /// POST请求时，数据是否进行gzip压缩
        /// </summary>
        public bool GZipCompress { get; set; }

        public bool KeepAlive { get; set; }

        public string CharacterSet { get; set; }

        public HttpConfig()
        {
            this.Timeout = 100000;
            this.ContentType = "text/html; charset=" + Encoding.UTF8.WebName;
            this.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/33.0.1750.117 Safari/537.36";
            this.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            this.AcceptEncoding = "gzip,deflate";
            this.GZipCompress = false;
            this.KeepAlive = true;
            this.CharacterSet = "UTF-8";
        }
    }

}
