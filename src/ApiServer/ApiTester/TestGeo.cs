using System;
using MongoDB.Driver.GeoJsonObjectModel;
using NUnit.Framework;
using WomPlatform.Web.Api;
using WomPlatform.Web.Api.DatabaseDocumentModels;

namespace ApiTester {

    public class TestGeo {

        [Test]
        public void TestSimpleCoords() {
            var c1 = new GeoJson2DGeographicCoordinates(23.45, 12.12);
            Assert.AreEqual(12.12, c1.Latitude);
            Assert.AreEqual(23.45, c1.Longitude);

            var c2 = new double[] { 34.34, 11.23 }.ToGeoCoord();
            Assert.AreEqual(34.34, c2.Latitude);
            Assert.AreEqual(11.23, c2.Longitude);

            Assert.Throws<ArgumentException>(() => new double[] { 34.34, 11.23, 45.45 }.ToGeoCoord());
            Assert.Throws<ArgumentException>(() => new double[] { 34.34 }.ToGeoCoord());
            Assert.Throws<ArgumentException>(() => new double[] { 90.01, 11.23 }.ToGeoCoord());
            Assert.Throws<ArgumentException>(() => new double[] { -90.01, 11.23 }.ToGeoCoord());
            Assert.Throws<ArgumentException>(() => new double[] { 34.34, 180.01 }.ToGeoCoord());
            Assert.Throws<ArgumentException>(() => new double[] { 34.34, -180.01 }.ToGeoCoord());
            Assert.Throws<ArgumentException>(() => new double[] { -110, -180.01 }.ToGeoCoord());
        }

        [Test]
        public void TestSimpleBounds() {
            var b1 = new Bounds {
                LeftTop = new GeoJson2DGeographicCoordinates(23.45, 12.12),
                RightBottom = new GeoJson2DGeographicCoordinates(45.67, -12.12)
            };
            Assert.AreEqual(12.12, b1.LeftTop.Latitude);
            Assert.AreEqual(23.45, b1.LeftTop.Longitude);
            Assert.AreEqual(-12.12, b1.RightBottom.Latitude);
            Assert.AreEqual(45.67, b1.RightBottom.Longitude);

            var b2 = new Bounds {
                LeftTop = new GeoJson2DGeographicCoordinates(-170, -85),
                RightBottom = new GeoJson2DGeographicCoordinates(175, 85)
            };
            Assert.AreEqual(-85, b2.LeftTop.Latitude);
            Assert.AreEqual(-170, b2.LeftTop.Longitude);
            Assert.AreEqual(85, b2.RightBottom.Latitude);
            Assert.AreEqual(175, b2.RightBottom.Longitude);
        }

        [Test]
        public void TestBoundsContains() {
            var normal = new Bounds {
                LeftTop = new GeoJson2DGeographicCoordinates(23.45, 12.12),
                RightBottom = new GeoJson2DGeographicCoordinates(45.67, -12.12)
            };
            Assert.AreEqual(true, normal.Contains(new GeoJson2DGeographicCoordinates(30, 10)));
            Assert.AreEqual(true, normal.Contains(new GeoJson2DGeographicCoordinates(25, -8)));
            Assert.AreEqual(false, normal.Contains(new GeoJson2DGeographicCoordinates(25, -20)));
            Assert.AreEqual(false, normal.Contains(new GeoJson2DGeographicCoordinates(50, -8)));
            Assert.AreEqual(false, normal.Contains(new GeoJson2DGeographicCoordinates(-160, 10)));

            var crossLong = new Bounds {
                LeftTop = new GeoJson2DGeographicCoordinates(160, 12.12),
                RightBottom = new GeoJson2DGeographicCoordinates(-160, -12.12)
            };
            Assert.AreEqual(true, crossLong.Contains(new GeoJson2DGeographicCoordinates(170, 5)));
            Assert.AreEqual(true, crossLong.Contains(new GeoJson2DGeographicCoordinates(-170, 5)));
            Assert.AreEqual(true, crossLong.Contains(new GeoJson2DGeographicCoordinates(160, -12.12)));
            Assert.AreEqual(true, crossLong.Contains(new GeoJson2DGeographicCoordinates(-160, 12.12)));
            Assert.AreEqual(false, crossLong.Contains(new GeoJson2DGeographicCoordinates(120, 5)));
            Assert.AreEqual(false, crossLong.Contains(new GeoJson2DGeographicCoordinates(-120, 5)));
            Assert.AreEqual(false, crossLong.Contains(new GeoJson2DGeographicCoordinates(-170, 30)));
            Assert.AreEqual(false, crossLong.Contains(new GeoJson2DGeographicCoordinates(170, -30)));

            var crossLat = new Bounds {
                LeftTop = new GeoJson2DGeographicCoordinates(23.45, -80),
                RightBottom = new GeoJson2DGeographicCoordinates(45.67, 70)
            };
            Assert.AreEqual(true, crossLat.Contains(new GeoJson2DGeographicCoordinates(32.45, -85)));
            Assert.AreEqual(true, crossLat.Contains(new GeoJson2DGeographicCoordinates(32.45, 70)));
            Assert.AreEqual(true, crossLat.Contains(new GeoJson2DGeographicCoordinates(23.45, -80)));
            Assert.AreEqual(true, crossLat.Contains(new GeoJson2DGeographicCoordinates(45.67, 89.5)));
            Assert.AreEqual(false, crossLat.Contains(new GeoJson2DGeographicCoordinates(30.12, -70)));
            Assert.AreEqual(false, crossLat.Contains(new GeoJson2DGeographicCoordinates(30.12, 30)));
            Assert.AreEqual(false, crossLat.Contains(new GeoJson2DGeographicCoordinates(10.05, -85)));
            Assert.AreEqual(false, crossLat.Contains(new GeoJson2DGeographicCoordinates(52.17, 5)));

            var crossBoth = new Bounds {
                LeftTop = new GeoJson2DGeographicCoordinates(150, -80),
                RightBottom = new GeoJson2DGeographicCoordinates(-150, 80)
            };
            Assert.AreEqual(true, crossBoth.Contains(new GeoJson2DGeographicCoordinates(165, -85)));
            Assert.AreEqual(true, crossBoth.Contains(new GeoJson2DGeographicCoordinates(165, 85)));
            Assert.AreEqual(true, crossBoth.Contains(new GeoJson2DGeographicCoordinates(-185, -89.9)));
            Assert.AreEqual(true, crossBoth.Contains(new GeoJson2DGeographicCoordinates(-189.9, 89.9)));
            Assert.AreEqual(false, crossBoth.Contains(new GeoJson2DGeographicCoordinates(0, 0)));
            Assert.AreEqual(false, crossBoth.Contains(new GeoJson2DGeographicCoordinates(123, -78)));
            Assert.AreEqual(false, crossBoth.Contains(new GeoJson2DGeographicCoordinates(-123, 78)));
            Assert.AreEqual(false, crossBoth.Contains(new GeoJson2DGeographicCoordinates(120, -80)));
            Assert.AreEqual(false, crossBoth.Contains(new GeoJson2DGeographicCoordinates(-120, 82.12)));
        }

    }

}
