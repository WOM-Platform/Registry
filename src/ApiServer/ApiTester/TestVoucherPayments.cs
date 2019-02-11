using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Org.BouncyCastle.Crypto;
using RestSharp;
using WomPlatform.Web.Api;
using WomPlatform.Web.Api.Models;

namespace ApiTester {

    public class TestVoucherPayments {

        protected RestClient Client {
            get {
                return new RestClient($"http://wom.social/api/v1");
            }
        }

        protected CryptoProvider Crypto { get; } = new CryptoProvider(new ConsoleLogger<CryptoProvider>());

        protected Random Random { get; } = new Random();

        protected RestRequest CreateJsonRequest(string urlPath, object jsonBody) {
            var request = new RestRequest(urlPath, Method.POST) {
                RequestFormat = DataFormat.Json
            };
            request.AddHeader("Accept", "application/json");
            if (jsonBody != null) {
                request.AddJsonBody(jsonBody);
            }
            return request;
        }

        protected T PerformRequest<T>(RestRequest request) {
            var response = Client.Execute(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK) {
                throw new InvalidOperationException(string.Format("API status code {0}", response.StatusCode));
            }

            return JsonConvert.DeserializeObject<T>(response.Content);
        }

        AsymmetricKeyParameter _keyPos, _keyInstrument, _keyRegistry;
        byte[] _keySession;

        [SetUp]
        public void Setup() {
            _keyPos = CryptoHelper.LoadKeyFromPem<AsymmetricCipherKeyPair>("pos1.pem").Private;
            _keyInstrument = CryptoHelper.LoadKeyFromPem<AsymmetricCipherKeyPair>("source1.pem").Private;
            _keyRegistry = CryptoHelper.LoadKeyFromPem<AsymmetricCipherKeyPair>("registry.pem").Public;

            var rnd = new Random();
            byte[] sessionKey = new byte[256 / 8];
            rnd.NextBytes(sessionKey);
            _keySession = sessionKey;
        }

        private Guid CreateVouchers(string password, params VoucherCreatePayload.VoucherInfo[] vouchers) {
            var nonce = Guid.NewGuid().ToString("N");
            var request = CreateJsonRequest("voucher/create", new VoucherCreatePayload {
                SourceId = 1,
                Nonce = nonce,
                Payload = Crypto.Encrypt(new VoucherCreatePayload.Content {
                    SourceId = 1,
                    Nonce = nonce,
                    Password = password,
                    Vouchers = vouchers
                }, _keyRegistry)
            });

            var response = PerformRequest<VoucherCreateResponse>(request);
            var responseContent = Crypto.Decrypt<VoucherCreateResponse.Content>(response.Payload, _keyInstrument);

            request = CreateJsonRequest("voucher/verify", new VoucherVerifyPayload {
                Payload = Crypto.Encrypt(new VoucherVerifyPayload.Content {
                    Otc = responseContent.Otc
                }, _keyRegistry)
            });
            PerformRequest<VoucherCreateResponse>(request);

            return responseContent.Otc;
        }

        private VoucherRedeemResponse.VoucherInfo[] RedeemVouchers(Guid otc, string password) {
            var request = CreateJsonRequest("voucher/redeem", new VoucherRedeemPayload {
                Payload = Crypto.Encrypt(new VoucherRedeemPayload.Content {
                    Otc = otc,
                    Password = password,
                    SessionKey = _keySession.ToBase64()
                }, _keyRegistry)
            });

            var response = PerformRequest<VoucherRedeemResponse>(request);
            var responseContent = Crypto.Decrypt<VoucherRedeemResponse.Content>(response.Payload, _keySession);

            return responseContent.Vouchers;
        }

        private VoucherCreatePayload.VoucherInfo[] CreateRandomVoucherRequests(int count, string aimCode = null) {
            var now = DateTime.UtcNow;
            var voucherInfos = new List<VoucherCreatePayload.VoucherInfo>();
            for (int i = 0; i < count; ++i) {
                voucherInfos.Add(new VoucherCreatePayload.VoucherInfo {
                    Aim = aimCode ?? "1",
                    Latitude = Random.NextBetween(5, 40),
                    Longitude = Random.NextBetween(5, 50),
                    Timestamp = now
                });

                now = now.Subtract(TimeSpan.FromMinutes(30));
            }
            return voucherInfos.ToArray();
        }

        private Guid CreatePayment(string password, int amount, string pocketAckUrl, SimpleFilter filter = null) {
            var nonce = Guid.NewGuid().ToString("N");
            var request = CreateJsonRequest("payment/register", new PaymentRegisterPayload {
                PosId = 1,
                Nonce = nonce,
                Payload = Crypto.Encrypt(new PaymentRegisterPayload.Content {
                    PosId = 1,
                    Nonce = nonce,
                    Password = password,
                    Amount = amount,
                    PosAckUrl = string.Format("https://example.org/pos/test/{0:N}", nonce),
                    PocketAckUrl = pocketAckUrl,
                    SimpleFilter = filter
                }, _keyRegistry)
            });

            var response = PerformRequest<PaymentRegisterResponse>(request);
            var responseContent = Crypto.Decrypt<PaymentRegisterResponse.Content>(response.Payload, _keyPos);

            request = CreateJsonRequest("payment/verify", new PaymentVerifyPayload {
                Payload = Crypto.Encrypt(new PaymentVerifyPayload.Content {
                    Otc = responseContent.Otc
                }, _keyRegistry)
            });

            PerformRequest<VoucherCreateResponse>(request);

            return responseContent.Otc;
        }

        private string ProcessPayment(Guid otc, string password, params PaymentConfirmPayload.VoucherInfo[] vouchers) {
            var request = CreateJsonRequest("payment/confirm", new PaymentConfirmPayload {
                Payload = Crypto.Encrypt(new PaymentConfirmPayload.Content {
                    Otc = otc,
                    Password = password,
                    SessionKey = _keySession.ToBase64(),
                    Vouchers = vouchers
                }, _keyRegistry)
            });

            var response = PerformRequest<PaymentConfirmResponse>(request);
            var responseContent = Crypto.Decrypt<PaymentConfirmResponse.Content>(response.Payload, _keySession);

            return responseContent.AckUrl;
        }

        [Test]
        public void CreateAndRedeemRandomVouchers() {
            var voucherInfos = CreateRandomVoucherRequests(Random.Next(5) + 5);
            var otcGen = CreateVouchers("1234", voucherInfos);
            var vouchers = RedeemVouchers(otcGen, "1234");

            Assert.AreEqual(voucherInfos.Length, vouchers.Length);
            Console.WriteLine("Redeemed vouchers {0}", string.Join(", ", from v in vouchers select v.Id));

            var zip = (from v in voucherInfos orderby v.Timestamp select v)
                .Zip(from v in vouchers orderby v.Timestamp select v, (a, b) => (a, b));
            foreach((var a, var b) in zip) {
                Assert.IsTrue(b.Aim.EndsWith(a.Aim));
                Assert.AreEqual(a.Latitude, b.Latitude);
                Assert.AreEqual(a.Longitude, b.Longitude);
                Assert.AreEqual(a.Timestamp.ToSecondPrecision(), b.Timestamp);
            }
        }

        [Test]
        public void CreateVouchersAndProcessSimplePayment() {
            var voucherInfos = CreateRandomVoucherRequests(5);
            var otcGen = CreateVouchers("1234", voucherInfos);
            var vouchers = RedeemVouchers(otcGen, "1234");

            Assert.AreEqual(5, vouchers.Length);

            var ackUrl = string.Format("http://www.example.org/confirmation/{0:N}", Guid.NewGuid());

            var payOtc = CreatePayment("2345", 5, ackUrl);

            string returnAckUrl = ProcessPayment(payOtc, "2345", (from v in vouchers
                                            select new PaymentConfirmPayload.VoucherInfo {
                                                Id = v.Id,
                                                Secret = v.Secret
                                            }).ToArray());

            Assert.AreEqual(ackUrl, returnAckUrl);
        }

        [Test]
        public void FailedPaymentInsufficientVouchers() {
            var voucherInfos = CreateRandomVoucherRequests(1);
            var otcGen = CreateVouchers("1234", voucherInfos);
            var vouchers = RedeemVouchers(otcGen, "1234");

            Assert.AreEqual(1, vouchers.Length);

            var ackUrl = string.Format("http://www.example.org/confirmation/{0:N}", Guid.NewGuid());

            var payOtc = CreatePayment("2345", 2, ackUrl);

            Assert.Throws<InvalidOperationException>(() => {
                ProcessPayment(payOtc, "2345", (from v in vouchers
                                                select new PaymentConfirmPayload.VoucherInfo {
                                                    Id = v.Id,
                                                    Secret = v.Secret
                                                }).ToArray());
            });
        }

        [Test]
        public void FailedPaymentWrongAim() {
            var otcGen = CreateVouchers("1234",
                new VoucherCreatePayload.VoucherInfo {
                    Aim = "1/1",
                    Latitude = 12,
                    Longitude = 12,
                    Timestamp = DateTime.UtcNow
                },
                new VoucherCreatePayload.VoucherInfo {
                    Aim = "2",
                    Latitude = 12,
                    Longitude = 12,
                    Timestamp = DateTime.UtcNow
                }
            );
            var vouchers = RedeemVouchers(otcGen, "1234");

            Assert.AreEqual(2, vouchers.Length);
            Assert.IsTrue(vouchers[0].Aim.EndsWith("/1/1"));
            Assert.IsTrue(vouchers[1].Aim.EndsWith("/2"));

            var ackUrl = string.Format("http://www.example.org/confirmation/{0:N}", Guid.NewGuid());

            var payOtc = CreatePayment("2345", 2, ackUrl, new SimpleFilter {
                Aim = "1"
            });

            Assert.Throws<InvalidOperationException>(() => {
                ProcessPayment(payOtc, "2345", (from v in vouchers
                                                select new PaymentConfirmPayload.VoucherInfo {
                                                    Id = v.Id,
                                                    Secret = v.Secret
                                                }).ToArray());
            });
        }

    }

}
