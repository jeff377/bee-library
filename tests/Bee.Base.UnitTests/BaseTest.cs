using System.Text;

namespace Bee.Base.UnitTests
{
    public class BaseTest
    {
        /// <summary>
        /// IP ��}���ҡC
        /// </summary>
        [Fact]
        public void IPValidator()
        {
            // �w�q�զW��
            var whitelist = new List<string>
            {
                "192.168.1.*",
                "10.0.*.*",
                "192.168.2.0/24"
            };

            // �w�q�¦W��
            var blacklist = new List<string>
            {
                "192.168.1.100",
                "10.0.0.5",
                "192.168.3.0/24"
            };

            // ��l�����Ҿ�
            var validator = new TIPValidator(whitelist, blacklist);

            // �ˬd IP �a�}�O�_�Q���\
            var allowed = validator.IsIpAllowed("192.168.2.50");
            Assert.True(allowed);  // ����^�ǭȻP�w����
            var allowed2 = validator.IsIpAllowed("10.0.0.5");
            Assert.False(allowed2);  // ����^�ǭȻP�w����
        }

        /// <summary>
        /// �[�ѱK���աC
        /// </summary>
        [Fact]
        public void Encryption()
        {
            byte[] oSrcBytes;
            byte[] oDstBytes;
            string sSrcValue;
            string sDstValue;
            string sKey;
            string sIV;
            string sEncryption;

            sKey = StrFunc.Left(Guid.NewGuid().ToString().Replace("-", ""), 32);
            sIV = StrFunc.Left(Guid.NewGuid().ToString().Replace("-", ""), 16);

            sSrcValue = "���Y���դ�r";
            oSrcBytes = Encoding.UTF8.GetBytes(sSrcValue);
            oDstBytes = EncryptionFunc.AesEncrypt(oSrcBytes, sKey, sIV);
            oSrcBytes = EncryptionFunc.AesDecrypt(oDstBytes, sKey, sIV);
            sDstValue = Encoding.UTF8.GetString(oSrcBytes);
            Assert.Equal(sSrcValue, sDstValue);

            oSrcBytes = Encoding.UTF8.GetBytes(sSrcValue);
            oDstBytes = EncryptionFunc.AesEncrypt(oSrcBytes);
            oSrcBytes = EncryptionFunc.AesDecrypt(oDstBytes);
            sDstValue = Encoding.UTF8.GetString(oSrcBytes);
            Assert.Equal(sSrcValue, sDstValue);

            sEncryption = EncryptionFunc.AesEncrypt(sSrcValue);
            sDstValue = EncryptionFunc.AesDecrypt(sEncryption);
            Assert.Equal(sSrcValue, sDstValue);

            sDstValue = EncryptionFunc.Sha512Encrypt(sSrcValue);
            Assert.NotEmpty(sDstValue);

            sDstValue = EncryptionFunc.Sha256Encrypt(sSrcValue);
            Assert.NotEmpty(sDstValue);
        }
    }
}