using System;
using System.Net;

namespace WallpaperTool.Core
{
    /// <summary>
    /// .NET Framework 4.0 默认只启用 Ssl3/Tls1.0，访问 picsum.photos 这类只支持 TLS 1.2 的站点
    /// 会直接握手失败。这里按数值打开 TLS 1.2：.NET 4.0 的枚举里没有该成员，
    /// 用 3072 兼容 4.5+ 运行时，老运行时则保持默认。
    /// </summary>
    internal static class NetworkSecurity
    {
        private const int Tls12 = 3072;

        public static void EnableModernTls()
        {
            try
            {
                ServicePointManager.SecurityProtocol |= (SecurityProtocolType)Tls12;
            }
            catch
            {
                // 运行时太老、不支持 TLS 1.2 时忽略
            }
        }
    }
}
