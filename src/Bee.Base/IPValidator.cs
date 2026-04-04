using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Bee.Base
{
    /// <summary>
    /// IP 位址合法性驗證器。
    /// </summary>
    /// <remarks>
    /// 支援星號 (*) 來表示通配符，例如 192.168.1.*
    /// 支援子網掩碼 (/) 來表示 IP 範圍，例如 192.168.2.0/24
    /// </remarks>
    public class IPValidator
    {
        private readonly List<string> _whitelist;
        private readonly List<string> _blacklist;

        /// <summary>
        /// 建構函式。
        /// </summary>
        /// <param name="whitelist">白名單 IP 位址模式的列表。</param>
        /// <param name="blacklist">黑名單 IP 位址模式的列表。</param>
        public IPValidator(List<string> whitelist, List<string> blacklist)
        {
            _whitelist = whitelist ?? new List<string>();
            _blacklist = blacklist ?? new List<string>();
        }

        /// <summary>
        /// 白名單 IP 位址模式的列表。
        /// </summary>
        public List<string> Whitelist
        {
            get { return _whitelist; }
        }

        /// <summary>
        /// 黑名單 IP 位址模式的列表。
        /// </summary>
        public List<string> Blacklist
        {
            get { return _blacklist; }
        }

        /// <summary>
        /// 檢查給定的 IP 位址是否被允許（基於白名單和黑名單）。
        /// </summary>
        /// <param name="ipAddress">要檢查的 IP 位址。</param>
        /// <returns>如果 IP 位址被允許則返回 true，否則返回 false。</returns>
        public bool IsIpAllowed(string ipAddress)
        {
            // 檢查 IP 位址是否在黑名單中
            if (IsIpBlacklisted(ipAddress))
            {
                return false;
            }

            // 檢查 IP 位址是否在白名單中
            return IsIpWhitelisted(ipAddress);
        }

        /// <summary>
        /// 檢查給定的 IP 位址是否在白名單中。
        /// </summary>
        /// <param name="ipAddress">要檢查的 IP 位址。</param>
        /// <returns>如果 IP 位址在白名單中則返回 true，否則返回 false。</returns>
        private bool IsIpWhitelisted(string ipAddress)
        {
            foreach (string pattern in this.Whitelist)
            {
                if (IsMatch(ipAddress, pattern))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 檢查給定的 IP 位址是否在黑名單中。
        /// </summary>
        /// <param name="ipAddress">要檢查的 IP 位址。</param>
        /// <returns>如果 IP 位址在黑名單中則返回 true，否則返回 false。</returns>
        private bool IsIpBlacklisted(string ipAddress)
        {
            foreach (string pattern in this.Blacklist)
            {
                if (IsMatch(ipAddress, pattern))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 檢查給定的 IP 位址是否符合指定的模式（支持通配符和 CIDR 表示法）。
        /// </summary>
        /// <param name="ipAddress">要檢查的 IP 位址。</param>
        /// <param name="pattern">要匹配的模式。</param>
        /// <returns>如果 IP 位址符合模式則返回 true，否則返回 false。</returns>
        private bool IsMatch(string ipAddress, string pattern)
        {
            // 檢查模式是否為 CIDR 表示法
            if (pattern.Contains('/'))
            {
                return IsInSubnet(IPAddress.Parse(ipAddress), pattern);
            }

            // 否則，使用通配符匹配
            return IsWildcardMatch(ipAddress, pattern);
        }

        /// <summary>
        /// 檢查給定的 IP 位址是否符合通配符模式。
        /// </summary>
        /// <param name="ipAddress">要檢查的 IP 位址。</param>
        /// <param name="pattern">要匹配的通配符模式。</param>
        /// <returns>如果 IP 位址符合通配符模式則返回 true，否則返回 false。</returns>
        private bool IsWildcardMatch(string ipAddress, string pattern)
        {
            string[] ipParts = ipAddress.Split('.');
            string[] patternParts = pattern.Split('.');

            for (int i = 0; i < ipParts.Length; i++)
            {
                if (patternParts[i] == "*") continue;
                if (ipParts[i] != patternParts[i]) return false;
            }
            return true;
        }

        /// <summary>
        /// 檢查給定的 IP 位址是否在指定的 CIDR 子網內。
        /// </summary>
        /// <param name="address">要檢查的 IP 位址。</param>
        /// <param name="cidr">要匹配的 CIDR 子網模式。</param>
        /// <returns>如果 IP 位址在子網內則返回 true，否則返回 false。</returns>
        private bool IsInSubnet(IPAddress address, string cidr)
        {
            string[] parts = cidr.Split('/');
            IPAddress ipAddress = IPAddress.Parse(parts[0]);
            int prefixLength = int.Parse(parts[1]);

            uint mask = uint.MaxValue << (32 - prefixLength);
            uint ipAddressBits = BitConverter.ToUInt32(ipAddress.GetAddressBytes().Reverse().ToArray(), 0);
            uint addressBits = BitConverter.ToUInt32(address.GetAddressBytes().Reverse().ToArray(), 0);

            return (ipAddressBits & mask) == (addressBits & mask);
        }
    }

}



