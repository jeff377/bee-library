using System.ComponentModel;
using Bee.Business.System;
using Bee.Definition;

namespace Bee.Business.UnitTests
{
    /// <summary>
    /// Bee.Business.System 的 Args/Result DTO 預設值與屬性 round-trip 測試。
    /// </summary>
    public class SystemDtosTests
    {
        private static readonly string[] s_keysK1K2 = { "k1", "k2" };
        private static readonly string[] s_keysX = { "x" };

        [Fact]
        [DisplayName("PingArgs 預設值與屬性 round-trip")]
        public void PingArgs_Defaults_And_RoundTrip()
        {
            var args = new PingArgs();
            Assert.Null(args.ClientName);
            Assert.Null(args.TraceId);

            args.ClientName = "c1";
            args.TraceId = "t1";
            Assert.Equal("c1", args.ClientName);
            Assert.Equal("t1", args.TraceId);
        }

        [Fact]
        [DisplayName("PingResult Status 預設為 ok 並可 round-trip")]
        public void PingResult_Defaults_And_RoundTrip()
        {
            var result = new PingResult();
            Assert.Equal("ok", result.Status);
            Assert.True(result.ServerTime <= DateTime.UtcNow.AddSeconds(1));
            Assert.Null(result.Version);
            Assert.Null(result.TraceId);

            var now = DateTime.UtcNow;
            result.Status = "pong";
            result.ServerTime = now;
            result.Version = "1.2.3";
            result.TraceId = "t1";

            Assert.Equal("pong", result.Status);
            Assert.Equal(now, result.ServerTime);
            Assert.Equal("1.2.3", result.Version);
            Assert.Equal("t1", result.TraceId);
        }

        [Fact]
        [DisplayName("LoginArgs 預設值與屬性 round-trip")]
        public void LoginArgs_Defaults_And_RoundTrip()
        {
            var args = new LoginArgs();
            Assert.Equal(string.Empty, args.UserId);
            Assert.Equal(string.Empty, args.Password);
            Assert.Equal(string.Empty, args.ClientPublicKey);

            args.UserId = "u";
            args.Password = "p";
            args.ClientPublicKey = "k";
            Assert.Equal("u", args.UserId);
            Assert.Equal("p", args.Password);
            Assert.Equal("k", args.ClientPublicKey);
        }

        [Fact]
        [DisplayName("LoginResult 預設值與屬性 round-trip")]
        public void LoginResult_Defaults_And_RoundTrip()
        {
            var result = new LoginResult();
            Assert.Equal(Guid.Empty, result.AccessToken);
            Assert.Equal(string.Empty, result.ApiEncryptionKey);
            Assert.Equal(string.Empty, result.UserId);
            Assert.Equal(string.Empty, result.UserName);

            var token = Guid.NewGuid();
            var expiry = DateTime.UtcNow.AddHours(1);
            result.AccessToken = token;
            result.ExpiredAt = expiry;
            result.ApiEncryptionKey = "enc";
            result.UserId = "u";
            result.UserName = "Name";

            Assert.Equal(token, result.AccessToken);
            Assert.Equal(expiry, result.ExpiredAt);
            Assert.Equal("enc", result.ApiEncryptionKey);
            Assert.Equal("u", result.UserId);
            Assert.Equal("Name", result.UserName);
        }

        [Fact]
        [DisplayName("CreateSessionArgs 預設 ExpiresIn=3600、OneTime=false")]
        public void CreateSessionArgs_Defaults()
        {
            var args = new CreateSessionArgs();
            Assert.Equal(string.Empty, args.UserID);
            Assert.Equal(3600, args.ExpiresIn);
            Assert.False(args.OneTime);
        }

        [Fact]
        [DisplayName("CreateSessionResult 預設 AccessToken 為 Empty")]
        public void CreateSessionResult_Defaults()
        {
            var result = new CreateSessionResult();
            Assert.Equal(Guid.Empty, result.AccessToken);
        }

        [Fact]
        [DisplayName("GetDefineArgs / GetDefineResult 預設值與 round-trip")]
        public void GetDefineArgsResult_RoundTrip()
        {
            var args = new GetDefineArgs
            {
                DefineType = DefineType.FormSchema,
                Keys = s_keysK1K2
            };
            Assert.Equal(DefineType.FormSchema, args.DefineType);
            Assert.Equal(s_keysK1K2, args.Keys);

            var result = new GetDefineResult { Xml = "<root/>" };
            Assert.Equal("<root/>", result.Xml);
        }

        [Fact]
        [DisplayName("SaveDefineArgs 預設值與 round-trip")]
        public void SaveDefineArgs_Defaults_And_RoundTrip()
        {
            var args = new SaveDefineArgs();
            Assert.Equal(string.Empty, args.Xml);
            Assert.Null(args.Keys);

            args.DefineType = DefineType.TableSchema;
            args.Xml = "<t/>";
            args.Keys = s_keysX;

            Assert.Equal(DefineType.TableSchema, args.DefineType);
            Assert.Equal("<t/>", args.Xml);
            Assert.Equal(s_keysX, args.Keys);
        }

        [Fact]
        [DisplayName("GetCommonConfigurationResult 預設為空字串並可 round-trip")]
        public void GetCommonConfigurationResult_Defaults_And_RoundTrip()
        {
            var result = new GetCommonConfigurationResult();
            Assert.Equal(string.Empty, result.CommonConfiguration);

            result.CommonConfiguration = "<c/>";
            Assert.Equal("<c/>", result.CommonConfiguration);
        }

    }
}
