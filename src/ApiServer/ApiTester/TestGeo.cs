using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using WomPlatform.Web.Api;

namespace ApiTester {

    public class TestGeo {

        [Test]
        public void TestSimpleCoords() {
            var c1 = new GeoCoords { Latitude = 12.12, Longitude = 23.45 };
            Assert.AreEqual(12.12, c1.Latitude);
            Assert.AreEqual(23.45, c1.Longitude);

            var c2 = new double[] { 34.34, 11.23 }.ToCoords();
            Assert.AreEqual(34.34, c2.Latitude);
            Assert.AreEqual(11.23, c2.Longitude);

            Assert.Throws<ArgumentException>(() => new double[] { 34.34, 11.23, 45.45 }.ToCoords());
            Assert.Throws<ArgumentException>(() => new double[] { 34.34 }.ToCoords());
            Assert.Throws<ArgumentException>(() => new double[] { 90.01, 11.23 }.ToCoords());
            Assert.Throws<ArgumentException>(() => new double[] { -90.01, 11.23 }.ToCoords());
            Assert.Throws<ArgumentException>(() => new double[] { 34.34, 180.01 }.ToCoords());
            Assert.Throws<ArgumentException>(() => new double[] { 34.34, -180.01 }.ToCoords());
            Assert.Throws<ArgumentException>(() => new double[] { -110, -180.01 }.ToCoords());
        }

        [Test]
        public void TestSimpleBounds() {
            var b1 = new GeoBounds {
                LeftTop = new GeoCoords { Latitude = 12.12, Longitude = 23.45 },
                RightBottom = new GeoCoords { Latitude = -12.12, Longitude = 45.67 }
            };
            Assert.AreEqual(12.12, b1.LeftTop.Latitude);
            Assert.AreEqual(23.45, b1.LeftTop.Longitude);
            Assert.AreEqual(-12.12, b1.RightBottom.Latitude);
            Assert.AreEqual(45.67, b1.RightBottom.Longitude);

            var b2 = new GeoBounds {
                LeftTop = new GeoCoords { Latitude = -85, Longitude = -170 },
                RightBottom = new GeoCoords { Latitude = 85, Longitude = 175 }
            };
            Assert.AreEqual(-85, b2.LeftTop.Latitude);
            Assert.AreEqual(-170, b2.LeftTop.Longitude);
            Assert.AreEqual(85, b2.RightBottom.Latitude);
            Assert.AreEqual(175, b2.RightBottom.Longitude);
        }

        [Test]
        public void TestBoundsContains() {
            var normal = new GeoBounds {
                LeftTop = new GeoCoords { Latitude = 12.12, Longitude = 23.45 },
                RightBottom = new GeoCoords { Latitude = -12.12, Longitude = 45.67 }
            };
            Assert.AreEqual(true, normal.Contains(new GeoCoords { Latitude = 10, Longitude = 30 }));
            Assert.AreEqual(true, normal.Contains(new GeoCoords { Latitude = -8, Longitude = 25 }));
            Assert.AreEqual(false, normal.Contains(new GeoCoords { Latitude = -20, Longitude = 25 }));
            Assert.AreEqual(false, normal.Contains(new GeoCoords { Latitude = -8, Longitude = 50 }));
            Assert.AreEqual(false, normal.Contains(new GeoCoords { Latitude = 10, Longitude = -160 }));

            var crossLong = new GeoBounds {
                LeftTop = new GeoCoords { Latitude = 12.12, Longitude = 160 },
                RightBottom = new GeoCoords { Latitude = -12.12, Longitude = -160 }
            };
            Assert.AreEqual(true, crossLong.Contains(new GeoCoords { Latitude = 5, Longitude = 170 }));
            Assert.AreEqual(true, crossLong.Contains(new GeoCoords { Latitude = 5, Longitude = -170 }));
            Assert.AreEqual(true, crossLong.Contains(new GeoCoords { Latitude = -12.12, Longitude = 160 }));
            Assert.AreEqual(true, crossLong.Contains(new GeoCoords { Latitude = 12.12, Longitude = -160 }));
            Assert.AreEqual(false, crossLong.Contains(new GeoCoords { Latitude = 5, Longitude = 120 }));
            Assert.AreEqual(false, crossLong.Contains(new GeoCoords { Latitude = 5, Longitude = -120 }));
            Assert.AreEqual(false, crossLong.Contains(new GeoCoords { Latitude = 30, Longitude = -170 }));
            Assert.AreEqual(false, crossLong.Contains(new GeoCoords { Latitude = -30, Longitude = 170 }));

            var crossLat = new GeoBounds {
                LeftTop = new GeoCoords { Latitude = -80, Longitude = 23.45 },
                RightBottom = new GeoCoords { Latitude = 70, Longitude = 45.67 }
            };
            Assert.AreEqual(true, crossLat.Contains(new GeoCoords { Latitude = -85, Longitude = 32.45 }));
            Assert.AreEqual(true, crossLat.Contains(new GeoCoords { Latitude = 70, Longitude = 32.45 }));
            Assert.AreEqual(true, crossLat.Contains(new GeoCoords { Latitude = -80, Longitude = 23.45 }));
            Assert.AreEqual(true, crossLat.Contains(new GeoCoords { Latitude = 89.5, Longitude = 45.67 }));
            Assert.AreEqual(false, crossLat.Contains(new GeoCoords { Latitude = -70, Longitude = 30.12 }));
            Assert.AreEqual(false, crossLat.Contains(new GeoCoords { Latitude = 30, Longitude = 30.12 }));
            Assert.AreEqual(false, crossLat.Contains(new GeoCoords { Latitude = -85, Longitude = 10.05 }));
            Assert.AreEqual(false, crossLat.Contains(new GeoCoords { Latitude = 5, Longitude = 52.17 }));

            var crossBoth = new GeoBounds {
                LeftTop = new GeoCoords { Latitude = -80, Longitude = 150 },
                RightBottom = new GeoCoords { Latitude = 80, Longitude = -150 }
            };
            Assert.AreEqual(true, crossBoth.Contains(new GeoCoords { Latitude = -85, Longitude = 165 }));
            Assert.AreEqual(true, crossBoth.Contains(new GeoCoords { Latitude = 85, Longitude = 165 }));
            Assert.AreEqual(true, crossBoth.Contains(new GeoCoords { Latitude = -89.9, Longitude = -185 }));
            Assert.AreEqual(true, crossBoth.Contains(new GeoCoords { Latitude = 89.9, Longitude = -189.9 }));
            Assert.AreEqual(false, crossBoth.Contains(new GeoCoords { Latitude = 0, Longitude = 0 }));
            Assert.AreEqual(false, crossBoth.Contains(new GeoCoords { Latitude = -78, Longitude = 123 }));
            Assert.AreEqual(false, crossBoth.Contains(new GeoCoords { Latitude = 78, Longitude = -123 }));
            Assert.AreEqual(false, crossBoth.Contains(new GeoCoords { Latitude = -80, Longitude = 120 }));
            Assert.AreEqual(false, crossBoth.Contains(new GeoCoords { Latitude = 82.12, Longitude = -120 }));
        }

    }

}
